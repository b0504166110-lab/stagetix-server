# 🎭 StageTix — שרת (ASP.NET Core Web API)

מערכת למכירת כרטיסים להופעות באודיטוריום, בסגנון "גלאטיקט". מנהלת האולם מפרסמת הופעות, וקונות בוחרות **כיסא מסוים** במפת האולם (שורה ומספר) ומזמינות עד 6 כיסאות בהזמנה אחת. השרת בנוי ב־ASP.NET Core 10 בארכיטקטורת שכבות, שומר את הנתונים ב־PostgreSQL, מאובטח ב־JWT, ומתעד את כל ה־API ב־Swagger.

לקוח React שמדגים את השרת נמצא ב־Repository נפרד: **stagetix-client**.

---

## ⭐ המשאב המוגבל והתחרות עליו

**המשאב:** כיסא מסוים בהופעה מסוימת (טבלת `ShowSeats`). לכל כיסא יש מצב אחד בלבד: `Available` או `Sold`.

**איפה התחרות:** ב־`POST /api/orders`. השרת קורא את הכיסאות, בודק שהם פנויים, מסמן אותם `Sold` ושומר. בין הקריאה לשמירה קונה אחרת יכולה לקנות את אותו כיסא. *"שתי קונות לחצו בו־זמנית על שורה 5 כיסא 12, שתיהן קיבלו אישור — ואסור היה ששתיהן יקבלו."*

**איך המערכת מגיבה (Optimistic Concurrency):**
1. לכל כיסא יש `Version` (Guid), שמוגדר כ־**Concurrency Token** (`IsConcurrencyToken` ב־Fluent API). `AppDbContext` מחליף אותו בכל שמירה.
2. EF Core שולח `UPDATE ... WHERE "Id" = @id AND "Version" = @versionWeRead`. אם מישהי שמרה קודם, אף שורה לא מתעדכנת, ונזרקת `DbUpdateConcurrencyException`.
3. `UnitOfWork` (שכבת Data) תופס **בדיוק** את החריגה הזו ומתרגם אותה ל־`ConcurrencyConflictException` של Core. כך שכבת Service לא תלויה ב־EF Core.
4. `OrderService` רושם **Warning** בלוג, וה־Middleware מחזיר **409 Conflict**. **כל ההזמנה נדחית**: הכול או כלום, כך שאף כיסא לא נמכר.
5. הבדיקה העסקית (הכיסאות פנויים ושייכים להופעה) והשמירה רצות **באותה טרנזקציה**.

**הוכחה:**
- `SeatConcurrencyTests`: שני `DbContext` נפרדים קוראים את אותו כיסא, הראשון שומר והשני מקבל `DbUpdateConcurrencyException`. הבדיקה רצה מול PostgreSQL אמיתי.
- מרוץ של 10 סבבים עם שני `OrderService` במקביל: בכל סבב נוצרה בדיוק הזמנה אחת.

---

## 🏗️ ארכיטקטורה

```
StageTix.sln
├── StageTix.Core      Entities, Enums, DTOs, Interfaces, Exceptions   ← לא מפנה לשום דבר
├── StageTix.Data      DbContext, Fluent API, Repositories, UnitOfWork, Migrations, Seed   → Core
├── StageTix.Service   לוגיקה עסקית, AutoMapper, JWT, Hash לסיסמאות                       → Core
├── StageTix.API       Controllers, Middleware, Program.cs, Swagger, NLog                → Service, Core (+Data לרישום בלבד)
└── StageTix.Tests     xUnit + Moq, בדיקות מול PostgreSQL, בדיקות ארכיטקטורה
```

- **כיוון התלויות נאכף בבדיקות** (`LayerDependencyTests`): ‏Core בלי שום הפניה חיצונית, ‏Service בלי EF Core, ושום Controller לא מזריק DbContext.
- **Program.cs קצר:** כל שכבה רושמת את עצמה (`AddDataLayer`, ‏`AddServiceLayer`), ושאר ההגדרות נמצאות ב־`StageTix.API/Extensions`.
- **בלי try/catch ב־Controllers:** ה־Services זורקים חריגות של Core, ו־`ExceptionHandlingMiddleware` ממפה אותן ל־400, ‏401, ‏404, ‏409 או 500, עם JSON אחיד:
  ```json
  { "status": 409, "title": "Conflict", "detail": "...", "correlationId": "...", "errors": null }
  ```

### ישויות וקשרים
| ישות | תפקיד | קשרים |
|---|---|---|
| `User` | קונה או מנהלת (Role) | 1:N → `Order` |
| `Show` | הופעה (קטגוריה, סטטוס Draft/OnSale/Cancelled) | 1:N → `ShowSeat`, ‏1:N → `Order` |
| `ShowSeat` ⭐ | כיסא בהופעה. **נושא את ה־Concurrency Token** | N:M ↔ `Order` |
| `Order` | הזמנה של 1–6 כיסאות | N:M ↔ `ShowSeat` דרך הטבלה `OrderSeats` |

**Migrations:** `InitialCreate` ו־`AddOrderCancelledAt`.

---

## 🔐 הרשאות

| פעולה | אורחת | קונה | מנהלת |
|---|:-:|:-:|:-:|
| רשימת הופעות, פרטי הופעה, מפת כיסאות | ✅ | ✅ | ✅ (כולל טיוטות) |
| הזמנת כיסאות | 401 | ✅ | 403 |
| ההזמנות שלי / פרטי הזמנה | 401 | רק שלה | ✅ כולן |
| יצירה, עריכה, פרסום, ביטול ומחיקה של הופעה | 401 | 403 | ✅ |
| ביטול הזמנה | 401 | 403 | ✅ |

- **מוגן כברירת מחדל:** כל Endpoint דורש Token, אלא אם הוא מסומן `[AllowAnonymous]`.
- זהות הקונה נקבעת **רק לפי ה־Token**, ולעולם לא לפי מזהה שהלקוח שולח.

## 📋 Endpoints

| Method | URL | הרשאה | תיאור |
|---|---|---|---|
| POST | `/api/users` | ציבורי | הרשמה (תמיד Customer) |
| POST | `/api/tokens` | ציבורי | התחברות, מחזירה JWT |
| GET | `/api/users/me` | מחוברת | הפרטים שלי |
| GET | `/api/users/{id}` | מחוברת | משתמשת (קונה: רק את עצמה) |
| GET | `/api/shows` | ציבורי | רשימה: `search`, `category`, `status` (מנהלת), `sortBy`, `sortDirection`, `page`, `pageSize` |
| GET | `/api/shows/{id}` | ציבורי | פרטי הופעה + ספירת כיסאות |
| GET | `/api/shows/{id}/seats` | ציבורי | מפת כיסאות |
| POST | `/api/shows` | Admin | יצירה (כטיוטה, יחד עם כל הכיסאות) |
| PUT | `/api/shows/{id}` | Admin | עריכה |
| PATCH | `/api/shows/{id}/status` | Admin | `OnSale` / `Cancelled` (ביטול מבטל גם את ההזמנות) |
| DELETE | `/api/shows/{id}` | Admin | מחיקת טיוטה |
| POST | `/api/orders` | Customer | ⭐ הזמנת כיסאות (201 או 409) |
| GET | `/api/orders` | מחוברת | רשימה עם דפדוף (קונה: רק שלה) |
| GET | `/api/orders/{id}` | מחוברת | פרטי הזמנה |
| PATCH | `/api/orders/{id}/status` | Admin | ביטול הזמנה ושחרור הכיסאות |
| GET | `/health` | ציבורי | בדיקת תקינות (כולל בסיס הנתונים) |

התיעוד המלא, עם קודי השגיאה, נמצא ב־**Swagger**: ‏`http://localhost:5248/swagger`.

---

## ▶️ הרצה מקומית

**דרישות:** ‏.NET SDK 10, ו־PostgreSQL (גרסה 14 ומעלה) שרץ על `localhost:5432`.

```powershell
cd server
dotnet tool restore                                     # מתקין dotnet-ef (גרסה נעולה ב־dotnet-tools.json)

# סודות — נשמרים מחוץ לפרויקט ולעולם לא נכנסים ל־Git:
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=stagetix;Username=postgres;Password=<הסיסמה שלך>" --project StageTix.API
dotnet user-secrets set "Jwt:SigningKey" "<מחרוזת אקראית של 32 תווים לפחות>" --project StageTix.API

dotnet run --project StageTix.API                       # http://localhost:5248/swagger
```

השרת **מפעיל בעצמו את ה־Migrations ואת ה־Seed** כשהוא עולה, ולכן אין צורך בשום פעולה ידנית בבסיס הנתונים. כדי להפעיל את ה־Migrations ידנית, משורש ה־Solution:
```powershell
dotnet ef database update -p StageTix.Data -s StageTix.API
```

**בדיקות:**
```powershell
dotnet test
```
3 בדיקות ה־Concurrency יוצרות בסיס נתונים זמני בשם `stagetix_tests` ומוחקות אותו בסוף. בלי PostgreSQL הן מדולגות (Skipped), ולא נכשלות.

### 👤 משתמשי דמו
| תפקיד | אימייל | סיסמה |
|---|---|---|
| מנהלת | `admin@example.com` | `Admin123!` |
| קונה | `dana@example.com` | `Customer123!` |
| קונה | `noa@example.com` | `Customer123!` |
| קונה | `maya@example.com` | `Customer123!` |

ה־Seed יוצר 25 הופעות (22 במכירה, 2 טיוטות, 1 שבוטלה), ו־**"ערב ניגונים — כמעט אזל!"** שבה נשארו רק **שורה 10, כיסאות 6 ו־7**. זו ההופעה להדגמת 409.

---

## ⚙️ הגדרות

| מפתח | איפה | הערות |
|---|---|---|
| `ConnectionStrings:Default` | User Secrets / `ConnectionStrings__Default` | 🔒 סוד. מקבל גם כתובת בפורמט `postgresql://...` של Neon או Render |
| `Jwt:SigningKey` | User Secrets / `Jwt__SigningKey` | 🔒 סוד, 32 תווים לפחות. בלעדיו השרת לא עולה |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | appsettings.json | תוקף ה־Token: 60 דקות |
| `Cors:AllowedOrigins` | appsettings / `Cors__AllowedOrigins__0` | כתובת הלקוח |
| `Database:SeedDemoData` | appsettings / `Database__SeedDemoData` | ברירת מחדל: `true` |
| `Hall:Rows`, `Hall:SeatsPerRow` | appsettings.json | 10×12 |
| `PORT` | משתנה סביבה | Render מגדיר אותו בעצמו |

**לוגים (NLog):** ‏`nlog.config` כותב לקונסול ולקובץ `logs/stagetix-<date>.log`. כל שורה כוללת את ה־**CorrelationId** של הבקשה, שמוחזר גם בכותרת `X-Correlation-ID`. התנגשות נרשמת ב־**Warning**, וקריסה ב־**Error**. סיסמאות, Tokens וגוף הבקשה **לעולם לא נכתבים ללוג**.

---

## 🛡️ הערות אבטחה ומגבלות ידועות

- **סיסמאות:** נשמרות כ־Hash בעזרת `PasswordHasher` של ASP.NET Core Identity (‏PBKDF2 עם salt).
- **התחברות שגויה:** "אימייל לא קיים" ו"סיסמה שגויה" מחזירים אותה הודעה בדיוק.
- **AutoMapper 14.0.0:** זו הגרסה האחרונה ברישיון MIT; גרסה 15 ומעלה דורשת מפתח רישיון. לגרסה יש התראת אבטחה ([GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x)) על קריסה כשממפים אובייקט שמפנה לעצמו בעומק של אלפי רמות. **היא לא רלוונטית כאן:** כל ה־DTOs שטוחים, ו־System.Text.Json מגביל את עומק ה־JSON שנכנס ל־64. ההתראה הושתקה **רק עבור הפגיעות הזו**, ב־`Directory.Build.props`.
- **משתמשי הדמו ידועים לכולם:** באתר ציבורי כל אחד יכול להתחבר כמנהלת. זה מקובל בפרויקט הדגמה. כדי לכבות את נתוני הדמו בפריסה אמיתית, הגדירי `Database__SeedDemoData=false`.
- **מקרה קצה:** אם המנהלת מבטלת הופעה בדיוק באותו רגע שבו נוצרת בה הזמנה, ההזמנה החדשה עלולה לא להתבטל. הכיסאות עצמם מוגנים במלואם.
