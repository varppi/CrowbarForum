<img width=900 src="https://github.com/user-attachments/assets/f5c5e69a-d678-428b-9778-b86ee318b2d3"/>

[![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
[![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)
[![Bootstrap](https://img.shields.io/badge/bootstrap-%238511FA.svg?style=for-the-badge&logo=bootstrap&logoColor=white)](https://img.shields.io/badge/bootstrap-%238511FA.svg?style=for-the-badge&logo=bootstrap&logoColor=white)

**Keywords for SEO:** No javascript , js free , privacy focused

### Crowbar Forum, a fully fledged somewhat secure, self hostable forum software built with C#, ASP.NET, Razor that *DOESN'T USE ANY JAVASCRIPT BY DEFAULT*. 
# Inspiration for the project
All the forums that don't use JS seem to be written in PHP, which is as a language laughably insecure, so I decided to make my own forum software striving to be more secure and modern than the PHP ones. I decided to go with ASP.NET, because it has good support for generating dynamic HTML content with Razor and, because it has a good inbuilt authentication and authorization system.
# Features and selling points
### Forum functions
- **Basic forum features like creating threads, comments etc...**
- **Altcha <img height=25 src="https://github.com/user-attachments/assets/792840a7-482d-470b-80f7-1bc93a184049"> POW catpcha integration (the only thing that uses JS in the whole project)**
- **Rate limits**
- **Admin only categories**
- **Can make threads visible to members only**
- **Customizable**
- **Markdown support**
### Security related
- **Built with security in mind, passed my own penetration tests**
- **WAF you can customize**
- **Access checks at every level reduces broken authorization vulnerabilities**
- **All JS blocked by server and enforced with a strict content security policy to prevent the effects of XSS and tracking** (unless captcha enabled)
- **Metadata, HTTP requests nor anyhting like that is logged**
- **Admin can wipe the forum of all data in case he suspects that an intruder is inside the server**

<hr><br><br>

# Screenshots
**Remember, you can customize the CSS yourself easily if you want to change things up a little.**
<br></br>
<img src="https://github.com/user-attachments/assets/0bb7e11d-fd51-4dae-8a8e-ad3d431f602d" width=750>
<img src="https://github.com/user-attachments/assets/c293050b-e544-40fe-8bef-98aca73784ce" width=750>
<img src="https://github.com/user-attachments/assets/c620257a-e010-4546-b705-74f114d10c71" width=750>
<img src="https://github.com/user-attachments/assets/031b3cd5-3c87-40f2-8384-9b2804a25d12" width=750>

<hr><br><br>

# Installation and usage
You can use dotnet on bare metal or use Docker. Docker is more recommended due to security reasons. It is also recommended to set up a reverse proxy like caddy if you are planning to run this as a clearnet site.

### Docker
```bash
git clone https://github.com/Varppi/CrowbarForum
cd CrowbarForum/Crowbar
docker build -t crowbar -f Crowbar/Dockerfile .
docker run --name crowbarforum_instance -v Crowbar/Database:/app/Database -v Crowbar/Settings:/app/Settings -p 8080:8080 crowbar
```
### Bare metal
```bash
git clone https://github.com/Varppi/CrowbarForum
cd CrowbarForum/Crowbar/Crowbar
dotnet tool install --global dotnet-ef 
export PATH=$PATH:~/.dotnet # Adding dotnet tool path to your $PATH variable (optional)
dotnet ef migrations remove 
dotnet ef migrations add ApplicationDbContextMigration
dotnet ef database update 
dotnet run
```

## Configuration
### Database
By default Crowbar Forum uses SQLite, but you can also use MSSQL or MySQL by changing the "database" field to `mysql/sqlite/mssql/postgre` and the "DefaultConnection" to match the format below:
#### Sqlite syntax
`Data Source=crowbarforum.db;`
#### MySQL, MSSQL and Postgresql syntax
`Server=myserver;Database=mydb;User=myuser;Password=mypassword;`

### WAF
Edit WafRegex.txt in the `Settings` directory.

### Encryption key
You can find the encryption key setting in the Settings/appsettings.json file. You can insert whatever password or random string you want, but if you put `RANDOMIZE_KEY` it will generate a random password at each startup. This also means that if you shut down the server, the database will be gone.

<hr><br><br>

# Security 
### Security architecture
![design](https://github.com/user-attachments/assets/cd80125d-237d-4241-9e94-19998165f9ae)

With security being the main focus on this project, a lot of thought went into it. Here's a list of all the security mechanisms.
- WAF sends a custom sized 404 page with a custom status code which makes directory bruteforcing much more tedious.
- Every action needs to be accompanied by a valid user claim making sure that even if an unauthenticated or unauthorized user managed to bypass the higher level security measures, the user still wouldn't be able to make anything happen.
- At no point does the program make custom SQL statements. Everything goes through Ef Core ORM.
- All user inputs are validated.
- Almost everything is stored in an encrypted form. The key is specified in appsettings.json.

> [!CAUTION]
> No software is safe and things always get overlooked. If you have any tech expertise, I urge you to go through the code yourself and report vulnerabilities if you find any.

### Security vulnerabilities most probable
The most probable vulnerabilities to be found in this project are probably DOS and incorrect authorization vulnerabilities due to the structure of the project. Most data that a view uses doesn't go through a getter which could lead to potentially sensitive information being sent to an attacker. I've of course taken action and looked through the code and fixed all the vulnerabilities I could find, but there might still be some left.

<hr><br><br>

# Roadmap
I'm just a single person making this, so it might take a while. Contributions are always welcome :)

### Priority 1
- [x] In rest encryption
- [x] Reply function
- [x] Likes
- [x] Easily customizable forum name
- [X] More customizable profile (description, pgp key etc)
- [x] Customizable WAF
- [ ] Clean up code
- [x] Support for hiding threads from non members
- [x] Setting to allow or disallow attachment downloads from non members
- [x] Admin only threads for announcements
- [x] Invite codes

### Priority 2
- [ ] Option to not have permanent encryption key, instead randomly generate one at startup
- [ ] Thread locking
- [ ] File storage
- [ ] Ranks
- [ ] Webhooks
