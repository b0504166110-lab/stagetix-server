# 🎭 StageTix — מתחילים כאן

ברוכה הבאה! המדריך הזה לוקח אותך מהקבצים שבמחשב ועד אתר שעובד באינטרנט, צעד אחר צעד.

```
final-project/
├── server/          ← שרת ASP.NET Core (ל־Repository בשם stagetix-server)
├── client/          ← לקוח React (ל־Repository בשם stagetix-client)
└── START-HERE.md    ← המדריך הזה
```

> ⚠️ **הערה חשובה על ממשקי האתרים:** את ההוראות ל־GitHub, ל־Neon ול־Render כתבתי לפי מה שידוע לי. אתרים משנים מדי פעם שמות של כפתורים ותפריטים. אם משהו לא נראה בדיוק כמו בתיאור, חפשי כפתור עם שם דומה. **הערכים** שצריך להזין (שמות משתנים, פקודות ונתיבים) נכונים בכל מקרה.

---

## חלק א — הרצה מקומית

### 1. כלים להתקנה
| כלי | למה | הורדה |
|---|---|---|
| **.NET SDK 10** | בנייה והרצה של השרת | https://dotnet.microsoft.com/download/dotnet/10.0 |
| **Node.js 20 ומעלה** | הרצה של הלקוח | https://nodejs.org (גרסת LTS) |
| **PostgreSQL 14 ומעלה** | בסיס הנתונים | https://www.postgresql.org/download/windows/ (רשמי בצד את הסיסמה שתבחרי!) |
| **Git** | העלאה ל־GitHub | https://git-scm.com/download/win |
| Visual Studio 2026 (לא חובה) | עריכת קוד נוחה | https://visualstudio.microsoft.com |

את `dotnet-ef` לא צריך להתקין בנפרד. הוא מוגדר בפרויקט, והפקודה `dotnet tool restore` שבהמשך מתקינה אותו.

### 2. הגדרת הסודות (User Secrets)
הסיסמה של בסיס הנתונים ומפתח ה־JWT **אסור שיופיעו בקוד או ב־GitHub**. בפיתוח שומרים אותם ב־User Secrets, שזו "כספת" במחשב שלך שנמצאת מחוץ לתיקיית הפרויקט.

פתחי PowerShell בתיקיית `server` והריצי:

```powershell
# 1) כתובת בסיס הנתונים. החליפי את <הסיסמה שלך> בסיסמת PostgreSQL
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=stagetix;Username=postgres;Password=<הסיסמה שלך>" --project StageTix.API

# 2) מפתח JWT אקראי. השורה הזו מייצרת מפתח חזק ושומרת אותו בלי להציג אותו
$b = New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String($b)) --project StageTix.API
```

### 3. Migrations: יצירת הטבלאות
השרת **מריץ את ה־Migrations לבד** כשהוא עולה, ולכן לא חייבים לעשות את זה ידנית. אם בכל זאת תרצי (מתיקיית `server`):

```powershell
dotnet tool restore
dotnet ef database update -p StageTix.Data -s StageTix.API
```
- `-p StageTix.Data`: הפרויקט שבו נמצאות ה־Migrations.
- `-s StageTix.API`: פרויקט ההפעלה, שממנו נקראות ההגדרות והסודות.

### 4. הפעלת השרת והלקוח
**חלון PowerShell ראשון: השרת**
```powershell
cd server
dotnet run --project StageTix.API
```
פתחי בדפדפן את http://localhost:5248. ייפתח Swagger ✅

**חלון PowerShell שני: הלקוח**
```powershell
cd client
copy .env.example .env
npm install
npm run dev
```
פתחי בדפדפן את http://localhost:5173 ✅

**משתמשי דמו:**
| תפקיד | אימייל | סיסמה |
|---|---|---|
| מנהלת | admin@example.com | Admin123! |
| קונה | dana@example.com | Customer123! |
| קונה | noa@example.com | Customer123! |

**הרצת הבדיקות:** בתיקיית `server` הריצי `dotnet test`. התוצאה הצפויה: 71 בדיקות עוברות.

---

## חלק ב — GitHub

### 1. איך רואים את ה־Commits של כל שלב
1. היכנסי ל־https://github.com/b0504166110-lab/stagetix-server
2. מעל רשימת הקבצים לחצי על **Commits** (יש שם סמל של שעון).
3. כל שלב בבנייה מופיע כ־Commit נפרד: `Step 1: ...` עד `Step 10: ...`.
4. לחיצה על Commit מציגה את כל השינויים שבו: **ירוק** הוא מה שנוסף, **אדום** הוא מה שנמחק.

### 2. איך בודקים שאין קבצים שאסור להעלות
ב־GitHub, בעמוד הראשי של כל Repository, ודאי שאין:
- תיקיות `bin`, ‏`obj` או `node_modules`
- קובץ `.env` (מותר רק `.env.example`)
- קובץ `secrets.json`, או כל סיסמה, Connection String או מפתח JWT

ואפשר לבדוק גם מקומית:
```powershell
git ls-files | Select-String "bin/|obj/|node_modules|\.env$|secrets"
```
אם הפקודה לא מדפיסה כלום, הכול תקין ✅

### 3. אם ה־Push לא בוצע: העלאה ידנית
בתיקיית `server`:
```powershell
git init -b main                 # רק אם אין עדיין תיקיית .git
git add -A
git commit -m "StageTix server"
git remote add origin https://github.com/b0504166110-lab/stagetix-server.git
git push -u origin main
```
ובתיקיית `client` אותו דבר, עם הכתובת `stagetix-client.git`.

אם `git remote add` עונה שהוא כבר קיים, דלגי על השורה הזו. בפעם הראשונה ייפתח חלון התחברות ל־GitHub.

---

## חלק ג — מסד נתונים בענן (PostgreSQL חינמי)

**ההמלצה: Neon** (https://neon.tech). יש לו מסלול חינמי שמתאים לפרויקט כזה, ובסיס הנתונים לא נמחק אחרי זמן קצוב.
(גם ל־Render יש PostgreSQL חינמי, אבל לפי מה שידוע לי בסיס נתונים כזה **נמחק אחרי כ־30 יום**. זה מסוכן אם הבדיקה של הפרויקט מתעכבת. בדקי את התנאים העדכניים באתר.)

### 1. יצירת בסיס נתונים
1. היכנסי ל־Neon והירשמי. אפשר להתחבר עם חשבון GitHub.
2. צרי **Project** חדש, למשל בשם `stagetix`. באזור (Region) בחרי אזור באירופה, למשל Frankfurt, כדי שיהיה קרוב לשרת.
3. בסיס הנתונים נוצר אוטומטית.

### 2. קבלת ה־Connection String
בדשבורד של הפרויקט לחצי **Connect**. תופיע כתובת שנראית כך:
```
postgresql://neondb_owner:AbC123xyz@ep-something-123.eu-central-1.aws.neon.tech/neondb?sslmode=require
```
**את הכתובת הזו אפשר להדביק כמו שהיא.** השרת יודע להמיר אותה לפורמט ש־Npgsql דורש.

### 3. שמירה בטוחה
- **אל תכניסי את הכתובת לקוד, ל־README או ל־GitHub.** יש בה סיסמה.
- שמרי אותה במקום בטוח (למשל מנהל סיסמאות), והדביקי אותה **רק** במשתני הסביבה של Render (בחלק ד).

### 4. חיבור השרת לבסיס הנתונים
השרת קורא את הכתובת ממשתנה הסביבה `ConnectionStrings__Default` (עם **שני** קווים תחתונים). בעלייה הראשונה הוא ייצור בעצמו את כל הטבלאות ואת נתוני הדמו.

---

## חלק ד — Render

> 💡 **מגבלה של המסלול החינמי:** שרת חינמי ב־Render "נרדם" אחרי כ־15 דקות בלי פעילות, וההתעוררות לוקחת עד כדקה. לפני הצגת הפרויקט, פתחי את `/health` כדי "להעיר" את השרת.

### 1. פתיחת חשבון
היכנסי ל־https://render.com, לחצי **Get Started** והירשמי **עם GitHub**. כך Render יקבל גישה ל־Repositories שלך.

### 2. השרת: Web Service מסוג Docker
1. בדשבורד: **New +** ← **Web Service**.
2. בחרי את ה־Repository `stagetix-server` (אם הוא לא מופיע, אשרי ל־Render גישה אליו).
3. מלאי:
   | שדה | ערך |
   |---|---|
   | Name | `stagetix-server` (השם יהיה חלק מהכתובת) |
   | Language / Runtime | **Docker** (Render מזהה את ה־`Dockerfile` לבד) |
   | Branch | `main` |
   | Region | Frankfurt (קרוב ל־Neon) |
   | Instance Type | **Free** |
4. תחת **Advanced** ← **Health Check Path** כתבי `/health`.

### 3. משתני סביבה של השרת
באותו מסך, תחת **Environment Variables**, הוסיפי:

| Key | Value |
|---|---|
| `ConnectionStrings__Default` | הכתובת מ־Neon (חלק ג) |
| `Jwt__SigningKey` | מפתח אקראי חדש, ראי למטה |
| `Cors__AllowedOrigins__0` | בינתיים השאירי ריק. נעדכן אחרי שהלקוח יעלה |
| `Database__SeedDemoData` | `true` |

**יצירת מפתח JWT** (בכל PowerShell). הפקודה מעתיקה את המפתח ישירות ללוח ההעתקה, בלי להציג אותו:
```powershell
$b = New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b) | Set-Clipboard
```
אחר כך הדביקי (Ctrl+V) בשדה Value של `Jwt__SigningKey`.

לחצי **Create Web Service**. ה־build הראשון לוקח כמה דקות. כשמופיע **Live**, העתיקי את הכתובת, למשל `https://stagetix-server.onrender.com`.

### 4. הלקוח: Static Site
1. **New +** ← **Static Site**, ובחרי את `stagetix-client`.
2. מלאי:
   | שדה | ערך |
   |---|---|
   | Name | `stagetix-client` |
   | Branch | `main` |
   | Build Command | `npm ci && npm run build` |
   | Publish Directory | `dist` |
3. **Environment Variables:**
   | Key | Value |
   |---|---|
   | `VITE_API_URL` | כתובת השרת, למשל `https://stagetix-server.onrender.com` (**בלי** `/` בסוף) |
4. לחצי **Create Static Site**, וכשמופיע **Live** העתיקי את כתובת הלקוח.

### 5. CORS: חיבור הלקוח לשרת
הדפדפן חוסם קריאות מאתר אחד לשרת באתר אחר, אלא אם השרת מתיר את זה במפורש.
1. חזרי ל־Web Service של השרת ← **Environment**.
2. ערכי את `Cors__AllowedOrigins__0` והכניסי את כתובת הלקוח, למשל `https://stagetix-client.onrender.com` (**בלי** `/` בסוף).
3. שמרי. השרת יעלה מחדש עם ההגדרה החדשה.

### 6. Deploy
- כל `git push` ל־`main` מפעיל Deploy אוטומטי. אפשר גם ללחוץ **Manual Deploy** ← **Deploy latest commit**.
- ⚠️ שינוי של `VITE_API_URL` בלקוח דורש Deploy חדש של הלקוח, כי הכתובת "נצרבת" בזמן ה־build.

### 7. בדיקה שהכול פעיל
| מה | איפה | מה אמור להופיע |
|---|---|---|
| השרת חי | `https://<שרת>/health` | `Healthy` |
| Swagger | `https://<שרת>/swagger` | רשימת כל ה־Endpoints |
| לוגים | Web Service ← **Logs** | שורות עם CorrelationId |
| הלקוח | `https://<לקוח>` | רשימת ההופעות |

---

## חלק ה — בדיקה מקצה לקצה

1. **Health Check:** פתחי את `https://<שרת>/health`. אמור להופיע `Healthy`.
2. **Swagger:** פתחי את `https://<שרת>/swagger`.
3. **התחברות מתוך Swagger:** פתחי `POST /api/tokens` ← **Try it out**, הדביקי
   ```json
   { "email": "admin@example.com", "password": "Admin123!" }
   ```
   ← **Execute**, והעתיקי את ה־`token` מהתשובה (הוא מתחיל ב־`eyJ`).
4. **Authorize:** לחצי על **Authorize** 🔓 למעלה, הדביקי **רק** את ה־token (בלי המילה Bearer) ← **Authorize** ← **Close**. עכשיו נסי `POST /api/shows`: אמור להתקבל **201**.
   - **401:** לחצי Logout ב־Authorize ונסי שוב. אמור להתקבל **401**.
   - **403:** התחברי כ־`dana@example.com` (‏`Customer123!`), שימי את ה־token שלה ב־Authorize ונסי `POST /api/shows`. אמור להתקבל **403**.
5. **התחברות מהלקוח:** פתחי את כתובת הלקוח ← **התחברות** ← לחצי "דנה (קונה)" ← **התחברי**.
6. **הדגמת 409, שתי קונות על המקום האחרון:**
   - פתחי חלון גלישה בסתר (Ctrl+Shift+N) והתחברי בו כ־`noa@example.com`.
   - בשני החלונות פתחי את "ערב ניגונים — כמעט אזל!" ובחרי **שורה 10, כיסאות 6 ו־7**.
   - לחצי "הזמיני" אצל דנה: 🎉 ההזמנה הצליחה.
   - לחצי "הזמיני" אצל נועה: 😕 נפתח חלון אדום **"המקום נתפס!"** עם HTTP 409.
   - כדי לאפס: התחברי כמנהלת ← **ניהול הזמנות** ← בטלי את ההזמנה של דנה.

---

## חלק ו — פתרון תקלות

| תקלה | מה רואים | פתרון |
|---|---|---|
| **Build Failed** (מקומי) | `error CS...` או `NETSDK1045` | ודאי ש־.NET SDK 10 מותקן (`dotnet --list-sdks`). הריצי `dotnet restore` ואז `dotnet build` מתיקיית `server` |
| **Build Failed ב־Render** | שגיאה בלוג של ה־build | ודאי שה־`Dockerfile` נמצא בשורש של `stagetix-server` ושבחרת Runtime מסוג Docker |
| **Connection String חסר** | `Connection string 'Default' is missing` | מקומית: הריצי שוב את פקודת `user-secrets` מחלק א. ב־Render: בדקי שהשם הוא `ConnectionStrings__Default`, עם שני קווים תחתונים |
| **Connection String שגוי** | `password authentication failed` או `No such host` | בדקי את הסיסמה ואת הכתובת. ב־Neon העתיקי שוב את הכתובת מ־**Connect** |
| **Migration לא רצה** | `relation "Shows" does not exist` | הריצי `dotnet ef database update -p StageTix.Data -s StageTix.API` מתיקיית `server` (קודם `dotnet tool restore`) |
| **שגיאת DateTime ב־PostgreSQL** | `Cannot write DateTime with Kind=Local/Unspecified to PostgreSQL type 'timestamp with time zone'` | בשרת כל התאריכים כבר ב־UTC. אם הוספת קוד, השתמשי ב־`DateTime.UtcNow` ולא ב־`DateTime.Now` |
| **מפתח JWT חסר** | `OptionsValidationException ... 'SigningKey' with the error: 'The SigningKey field is required.'` (או `must be at least 32 characters`) | הגדירי את `Jwt__SigningKey` (ב־Render) או את `Jwt:SigningKey` (ב־User Secrets), באורך 32 תווים לפחות |
| **שגיאת CORS** | בקונסול של הדפדפן (F12): `blocked by CORS policy` | ב־`Cors__AllowedOrigins__0` צריכה להיות בדיוק כתובת הלקוח, עם `https://` ו**בלי** `/` בסוף |
| **401 Unauthorized** | "A valid access token is required" | התחברי מחדש: ה־Token בתוקף 60 דקות. ב־Swagger הדביקי את ה־token בלי המילה Bearer |
| **403 Forbidden** | "You do not have permission" | זו התנהגות תקינה: הפעולה מיועדת לתפקיד אחר. קונה לא מנהלת, ומנהלת לא קונה כרטיסים |
| **Swagger לא נפתח** | 404 או דף ריק | הכתובת היא `/swagger`, עם אות קטנה. ודאי שהשרת רץ (פתחי קודם את `/health`) |
| **שרת לא עולה ב־Render** | הסטטוס נשאר Deploying או Failed | פתחי **Logs** וחפשי שורה עם `FATAL` או `fail`. בדרך כלל זה Connection String או מפתח JWT חסר |
| **השרת "נרדם"** | הבקשה הראשונה לוקחת כדקה | זו התנהגות רגילה במסלול החינמי. פתחי את `/health` וחכי |
| **הלקוח מציג "אין חיבור לשרת"** | הודעה אדומה | בדקי את `VITE_API_URL` ועשי Deploy מחדש ללקוח. ודאי שהשרת ער |
| **מירכאות "נבלעות" ב־psql** | `relation "__efmigrationshistory" does not exist` | ב־PostgreSQL יש הבדל בין אותיות גדולות לקטנות בשמות טבלאות, ו־PowerShell 5 מוחק מירכאות. כתבי את השאילתה לקובץ `.sql` והריצי `psql -f file.sql` |
