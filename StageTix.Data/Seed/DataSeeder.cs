using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Interfaces.Security;
using StageTix.Core.Options;

namespace StageTix.Data.Seed;

/// <summary>
/// Fills an empty database with invented demo data, so the system can be used right after it starts.
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

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
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
            ("ערב להיטים — כמעט אזל!", "להקת הגליל", "ערב שירים אהובים עם להקת הגליל. נשארו מקומות אחרונים.", ShowCategory.Music, 5, 180, ShowStatus.OnSale),
            ("צחוק עד הבוקר", "יואב שמש", "מופע סטנדאפ חדש על החיים בעיר הגדולה.", ShowCategory.StandUp, 7, 120, ShowStatus.OnSale),
            ("החלום של ליל קיץ", "תיאטרון הבמה הצעירה", "עיבוד מודרני ומצחיק לקומדיה הקלאסית.", ShowCategory.Theater, 9, 150, ShowStatus.OnSale),
            ("הדובי שאהב לשיר", "תיאטרון הכפית", "הצגה מוזיקלית לגילאי 3–8.", ShowCategory.Kids, 4, 60, ShowStatus.OnSale),
            ("ג'אז בחצות", "רביעיית הנמל", "ערב ג'אז אינטימי עם אלתורים וקלאסיקות.", ShowCategory.Music, 12, 140, ShowStatus.OnSale),
            ("אמא, אני בטלוויזיה", "שירה אדמון", "סטנדאפ על משפחה, ילדים וקבוצות וואטסאפ.", ShowCategory.StandUp, 14, 110, ShowStatus.OnSale),
            ("המלט — גרסה קצרה", "תיאטרון הגשר", "שעה וחצי של טרגדיה, חרבות ושאלות גדולות.", ShowCategory.Theater, 16, 170, ShowStatus.OnSale),
            ("הקוסם מארץ הקסמים", "להקת ניצנים", "מחזמר צבעוני לכל המשפחה.", ShowCategory.Kids, 11, 70, ShowStatus.OnSale),
            ("שירי ים ומדבר", "תזמורת חוף הים", "מסע מוזיקלי בין הים התיכון למדבר.", ShowCategory.Music, 19, 160, ShowStatus.OnSale),
            ("לילה של אלתורים", "קבוצת אין תסריט", "הקהל בוחר — השחקנים ממציאים.", ShowCategory.StandUp, 21, 100, ShowStatus.OnSale),
            ("בית בובות", "תיאטרון האור", "דרמה משפחתית עוצמתית.", ShowCategory.Theater, 23, 160, ShowStatus.OnSale),
            ("מסיבת פיג'מות", "רוני וטלי", "שירים וריקודים לקטנטנים.", ShowCategory.Kids, 18, 55, ShowStatus.OnSale),
            ("רוק ישראלי — מחווה", "להקת המעיין", "הלהיטים הגדולים של הרוק המקומי.", ShowCategory.Music, 26, 190, ShowStatus.OnSale),
            ("סטנדאפ בעברית קלה", "מיכאל ברק", "מופע לעולים חדשים וגם לוותיקים.", ShowCategory.StandUp, 28, 90, ShowStatus.OnSale),
            ("קומדיה של טעויות", "תיאטרון הבמה הצעירה", "תאומים, בלבול ושעה וחצי של צחוק.", ShowCategory.Theater, 30, 140, ShowStatus.OnSale),
            ("המדען הקטן", "מעבדת הקסם", "ניסויים מדעיים על הבמה לגילאי 6–12.", ShowCategory.Kids, 25, 65, ShowStatus.OnSale),
            ("קלאסיקה לאור נרות", "אנסמבל המיתרים", "רביעיית מיתרים מנגנת יצירות מוכרות.", ShowCategory.Music, 33, 200, ShowStatus.OnSale),
            ("אבא בהפסקה", "דני גל", "סטנדאפ על אבהות, עבודה ומילואים.", ShowCategory.StandUp, 35, 115, ShowStatus.OnSale),
            ("הנסיך הקטן", "תיאטרון הכפית", "עיבוד בימתי פיוטי לסיפור האהוב.", ShowCategory.Theater, 38, 130, ShowStatus.OnSale),
            ("הרפתקה בג'ונגל", "להקת ניצנים", "הצגה אינטראקטיבית עם בובות ענק.", ShowCategory.Kids, 40, 60, ShowStatus.OnSale),
            ("ערב שירי משוררים", "הדס ורון", "שירה מולחנת בליווי פסנתר.", ShowCategory.Music, 45, 150, ShowStatus.OnSale),
            ("מופע סוף השנה", "תיאטרון האור", "הפקה חדשה בהכנה.", ShowCategory.Theater, 60, 160, ShowStatus.Draft),
            ("פסטיבל הצחוק", "אמנים שונים", "ערב סטנדאפ משותף — בהכנה.", ShowCategory.StandUp, 75, 130, ShowStatus.Draft),
            ("ערב בלוז", "להקת הדלתא", "בוטל עקב מחלת הסולן.", ShowCategory.Music, 8, 150, ShowStatus.Cancelled),
            ("שלגיה והגמדים", "תיאטרון הכפית", "הצגת ילדים קלאסית עם שירים חדשים.", ShowCategory.Kids, 50, 60, ShowStatus.OnSale)
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
