using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Options;

namespace StageTix.Data.Seed;

/// <summary>
/// Fills an empty database with invented demo data (charity events, seminars and teshuva conferences), so the system can be used right after it starts.
/// Runs only when there are no users yet, so restarting the server never duplicates data.
/// </summary>
public class DataSeeder(
    AppDbContext db,
    IPasswordService passwordService,
    IOptions<HallOptions> hallOptions,
    TimeProvider timeProvider,
    ILogger<DataSeeder> logger)
{
    public const string AdminEmail = "admin@example.com";
    public const string AdminPassword = "Admin123!";
    public const string CustomerPassword = "Customer123!";

    private readonly HallOptions _hall = hallOptions.Value;

    public async Task SeedAsync(bool reset, CancellationToken cancellationToken)
    {
        if (reset)
        {
            // Order matters: orders reference shows and users, and seats are deleted with their show.
            await db.Orders.ExecuteDeleteAsync(cancellationToken);
            await db.Shows.ExecuteDeleteAsync(cancellationToken);
            await db.Users.ExecuteDeleteAsync(cancellationToken);
            logger.LogWarning("Database:ResetDemoData is on: all data was deleted and the demo data is seeded again");
        }

        if (await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogDebug("Database already contains data; demo seed skipped");
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var admin = CreateUser("מנהלת האולם", AdminEmail, AdminPassword, UserRole.Admin, now);
        var dana = CreateUser("דנה לוי", "dana@example.com", CustomerPassword, UserRole.Customer, now);
        var noa = CreateUser("נועה כהן", "noa@example.com", CustomerPassword, UserRole.Customer, now);
        var maya = CreateUser("מאיה מזרחי", "maya@example.com", CustomerPassword, UserRole.Customer, now);
        db.Users.AddRange(admin, dana, noa, maya);

        var shows = CreateShows(now);
        db.Shows.AddRange(shows);

        var customers = new[] { dana, noa, maya };
        var orders = new List<Order>();

        // Show #1 is "almost full": every seat is sold except two adjacent seats in the last row,
        // so two customers can easily race for the last seats.
        var almostFull = shows[0];
        var lastRow = _hall.Rows;
        var keepFree = new HashSet<int> { _hall.SeatsPerRow / 2, _hall.SeatsPerRow / 2 + 1 };
        var customerIndex = 0;
        foreach (var row in almostFull.Seats.GroupBy(s => s.Row))
        {
            var seatsToSell = row
                .Where(s => !(s.Row == lastRow && keepFree.Contains(s.Number)))
                .OrderBy(s => s.Number);

            // Each run of adjacent seats is sold in orders of up to 6 seats (the per-order limit).
            foreach (var run in AdjacentRuns(seatsToSell))
            {
                foreach (var chunk in run.Chunk(6))
                {
                    var buyer = customers[customerIndex++ % customers.Length];
                    orders.Add(CreateOrder(buyer, almostFull, chunk.ToList(), now.AddDays(-10)));
                }
            }
        }

        // A few regular orders in other shows.
        orders.Add(CreateOrder(dana, shows[1], SeatsAt(shows[1], row: 5, 6, 7), now.AddDays(-3)));
        orders.Add(CreateOrder(noa, shows[1], SeatsAt(shows[1], row: 5, 8, 9, 10), now.AddDays(-2)));
        orders.Add(CreateOrder(maya, shows[2], SeatsAt(shows[2], row: 1, 1, 2), now.AddDays(-1)));
        orders.Add(CreateOrder(dana, shows[3], SeatsAt(shows[3], row: 3, 4), now.AddDays(-1)));

        // A cancelled order: its seats are available again, but stay linked to the order as history.
        var cancelled = CreateOrder(noa, shows[2], SeatsAt(shows[2], row: 2, 5, 6), now.AddDays(-4));
        cancelled.Status = OrderStatus.Cancelled;
        cancelled.CancelledAt = now.AddDays(-2);
        foreach (var seat in cancelled.Seats)
        {
            seat.Status = SeatStatus.Available;
        }
        orders.Add(cancelled);

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo data seeded: {UserCount} users, {ShowCount} shows, {SeatCount} seats, {OrderCount} orders",
            4, shows.Count, shows.Sum(s => s.Seats.Count), orders.Count);
    }

    private User CreateUser(string fullName, string email, string password, UserRole role, DateTime now) => new()
    {
        FullName = fullName,
        Email = email,
        PasswordHash = passwordService.Hash(password),
        Role = role,
        CreatedAt = now.AddDays(-30)
    };

    private static Order CreateOrder(User user, Show show, List<ShowSeat> seats, DateTime createdAt)
    {
        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Sold;
        }

        return new Order
        {
            User = user,
            Show = show,
            Seats = seats,
            TotalPrice = show.TicketPrice * seats.Count,
            Status = OrderStatus.Active,
            CreatedAt = createdAt
        };
    }

    private static List<ShowSeat> SeatsAt(Show show, int row, params int[] numbers) =>
        show.Seats.Where(s => s.Row == row && numbers.Contains(s.Number)).ToList();

    /// <summary>Splits seats (sorted by number) into runs of adjacent seats: 1,2,3,5,6 → [1,2,3] [5,6].</summary>
    private static IEnumerable<List<ShowSeat>> AdjacentRuns(IEnumerable<ShowSeat> seats)
    {
        var run = new List<ShowSeat>();
        foreach (var seat in seats)
        {
            if (run.Count > 0 && seat.Number != run[^1].Number + 1)
            {
                yield return run;
                run = [];
            }
            run.Add(seat);
        }

        if (run.Count > 0)
        {
            yield return run;
        }
    }

    private List<Show> CreateShows(DateTime now)
    {
        // 18:00 UTC = 20:00 or 21:00 in Israel.
        var today = now.Date.AddHours(18);

        var data = new (string Title, string Artist, string Description, ShowCategory Category, int InDays, decimal Price, ShowStatus Status)[]
        {
            ("ערב צדקה גדול — כמעט אזל!", "הפקות חסדי לב", "ערב התרמה חסידי לטובת משפחות נזקקות לקראת החג, עם מקהלה, סיפור מרגש וכיבוד. נשארו מקומות אחרונים.", ShowCategory.Charity, 5, 180, ShowStatus.OnSale),
            ("כנס תשובה ארצי", "הפקות שבי נפש", "כנס חיזוק והתעוררות עם מגידי שיעור ורבנים מובילים, בליווי ניגוני תשובה.", ShowCategory.Teshuva, 7, 90, ShowStatus.OnSale),
            ("סמינר הורים: חינוך בדור הדיגיטלי", "מכון דרך הבית", "סמינר מעשי להורים ולמחנכים עם יועצים חינוכיים ורבנים. הכניסה לגברים.", ShowCategory.Seminar, 9, 120, ShowStatus.OnSale),
            ("ערב צדקה לנשים — בת מלך", "הפקות פנינים", "ערב התרמה לנשים לטובת כלות עניות, עם הרצאה מרכזית, הופעה מוזיקלית וארוחה קלה.", ShowCategory.Charity, 4, 150, ShowStatus.OnSale),
            ("הכנס השנתי לימי הרחמים", "הפקות קול הלב", "ערב התעוררות לקראת ימי הסליחות: שיעורים, פיוטים ותפילה משותפת.", ShowCategory.Teshuva, 12, 70, ShowStatus.OnSale),
            ("סמינר שלום בית", "מרכז בית נאמן", "סדנת העשרה לזוגות ולמתחתנים עם מדריכי שלום בית מוסמכים. לנשים בלבד.", ShowCategory.Seminar, 14, 110, ShowStatus.OnSale),
            ("ערב התרמה לישיבה הקדושה", "הפקות תורה ועבודה", "ערב עם גדולי מרביצי התורה, מנחה מוכר והופעת מקהלת ילדים לטובת מוסדות התורה.", ShowCategory.Charity, 16, 200, ShowStatus.OnSale),
            ("כנס תשובה לנוער", "הפקות אור חדש", "כנס מיוחד לבחורי ישיבות ולנוער, עם סיפורי חזרה בתשובה ומוזיקה.", ShowCategory.Teshuva, 11, 40, ShowStatus.OnSale),
            ("סמינר פרנסה בכבוד", "מכון נתיב הפרנסה", "יום עיון לבעלי עסקים קטנים ומחפשי עבודה עם יועצי עסקים ורבנים, כולל נטוורקינג.", ShowCategory.Seminar, 19, 100, ShowStatus.OnSale),
            ("ערב חסד לחולים", "ארגון ביקור חולים רחמים", "ערב התרמה לטובת מימון ציוד רפואי ולינה לבני משפחות חולים.", ShowCategory.Charity, 21, 160, ShowStatus.OnSale),
            ("כנס ההתעוררות לפני הימים הנוראים", "הפקות שערי תשובה", "כנס ענק עם מגידי שיעור מהשורה הראשונה, מוזיקה מלווה ותפילת נעילה.", ShowCategory.Teshuva, 23, 80, ShowStatus.OnSale),
            ("גאלה לטובת יתומים", "קרן אבות ובנים", "גאלה חגיגית עם קטעי הסרטה, חזנים ומקהלה לטובת תמיכה במשפחות יתומים.", ShowCategory.Charity, 26, 250, ShowStatus.OnSale),
            ("כנס בין אדם למקום", "הפקות חזון הלב", "יום חיזוק לגברים: שיעורים, התבודדות ותפילה בציבור.", ShowCategory.Teshuva, 28, 60, ShowStatus.OnSale),
            ("סמינר מחנכים: חינוך מתוך אהבה", "מכון חינוך בדרך", "יום עיון לגננות ולמלמדים על כלי עבודה חינוכיים ועל התמודדות עם ילדים מאתגרים.", ShowCategory.Seminar, 30, 130, ShowStatus.OnSale),
            ("ערב תמיכה בכולל אברכים", "קרן תורתו אומנותו", "ערב עם סיפורי יושבי אוהל, מקהלה ושיעור מרכזי לטובת כוללי אברכים.", ShowCategory.Charity, 25, 170, ShowStatus.OnSale),
            ("כנס תשובה לנשים", "הפקות לבבות", "כנס חיזוק לנשים בנושא שמירת הלשון, צניעות ואמונה, עם דוברות מובילות.", ShowCategory.Teshuva, 33, 70, ShowStatus.OnSale),
            ("סמינר שידוכים והיכרות", "מרכז בנין עדי עד", "סמינר ללמידת תהליך השידוך עם שדכנים ורבנים. לגברים ולנשים בנפרד.", ShowCategory.Seminar, 35, 100, ShowStatus.OnSale),
            ("ערב קמחא דפסחא", "ארגון חסדי אברהם", "ערב התרמה להפצת סלי מזון לנזקקים לקראת החג, בהשתתפות רבנים וחזנים.", ShowCategory.Charity, 38, 140, ShowStatus.OnSale),
            ("כנס ההתחזקות הגדול", "הפקות שב אל לבך", "יום שלם של שיעורים וחיזוק בנושא עבודת המידות וקבלות לשנה החדשה.", ShowCategory.Teshuva, 40, 50, ShowStatus.OnSale),
            ("סמינר בריאות הנפש", "מכון נפש חיה", "יום עיון עם פסיכולוגים ורבנים בנושא התמודדות עם לחץ ומשברים בחברה החרדית.", ShowCategory.Seminar, 45, 150, ShowStatus.OnSale),
            ("ערב צדקה סוף שנה", "הפקות חסדי לב", "ערב התרמה חגיגי, בהכנה.", ShowCategory.Charity, 60, 190, ShowStatus.Draft),
            ("כנס תשובה ארצי — חורף", "הפקות שבי נפש", "כנס חיזוק חורפי, בהכנה.", ShowCategory.Teshuva, 75, 90, ShowStatus.Draft),
            ("סמינר ראשי ישיבות", "מכון דרך הבית", "בוטל עקב מחלת המרצה הראשי.", ShowCategory.Seminar, 8, 120, ShowStatus.Cancelled),
            ("ערב צדקה לטובת בית כנסת", "הפקות פנינים", "ערב התרמה לשיפוץ בית הכנסת השכונתי, עם חזן ומקהלה.", ShowCategory.Charity, 50, 120, ShowStatus.OnSale)
        };

        var shows = new List<Show>();
        foreach (var d in data)
        {
            var show = new Show
            {
                Title = d.Title,
                Artist = d.Artist,
                Description = d.Description,
                Category = d.Category,
                StartsAt = today.AddDays(d.InDays),
                TicketPrice = d.Price,
                Status = d.Status,
                CreatedAt = now.AddDays(-20)
            };
            show.GenerateSeats(_hall.Rows, _hall.SeatsPerRow);
            shows.Add(show);
        }

        return shows;
    }
}
