using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PetShop.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BlogPosts",
                columns: new[] { "Id", "AuthorName", "Content", "CreatedDate", "ImageUrl", "Slug", "Summary", "Title" },
                values: new object[,]
                {
                    { 1, "Admin", "Heyvanlara qayğı göstərmək və onların ehtiyaclarını qarşılamaq hər bir cəmiyyətin məsuliyyətidir. Bu yazıda dörd pəncəli dostlarımıza necə dəstək ola biləcəyimizi və bu addımların ətraf mühitimizə necə müsbət təsir etdiyini araşdırırıq.\n\n1. Şəfqət və empatiya hissinin formalaşması: Heyvanlarla ünsiyyət insanlarda, xüsusilə uşaqlarda mərhəmət və məsuliyyət hissini inkişaf etdirir.\n2. Təbiət və biomüxtəlifliyin qorunması: Canlıların sağlam həyat mühitinə sahib olması ekosistemin tarazlığını təmin edir.\n3. Küçə heyvanlarına sığınacaq və yemək təminatı: Xüsusilə qış aylarında isti sığınacaq və quru yem onların həyatda qalması üçün həyati vacibdir.\n4. Baytarlıq və peyvəndlərin əhəmiyyəti: Müntəzəm peyvəndlər heyvanları təhlükəli infeksiyalardan qoruyur.\n5. Düzgün sahiblənmə mədəniyyəti: Heyvan almaq əvəzinə sığınacaqlardan sahiblənmək bir canı xilas etmək deməkdir.", new DateTime(2025, 2, 20, 10, 0, 0, 0, DateTimeKind.Utc), "/images/blog1.jpg", "10-reasons-to-be-helpful-towards-any-animals", "At the core of our practice is the idea that cities are the incubators of our greatest achievements, and the best hope for a sustainable future.", "10 Reasons to be helpful towards any animals" },
                    { 2, "Dr. Leyla", "Ev heyvanları danışa bilməsə də, onların davranışları bizə çox şey deyir. Aclıq siqnalını düzgün anlamaq onların həddindən artıq kökəlməsinin və ya qidasız qalmasının qarşısını alır.\n\n- Qabının yanında gözləmək və ya qabı döyəcləmək: Pişik və itlərin ən yayğın vərdişlərindən biridir.\n- Narahatlıq və diqqət axtarışı: Əgər ev heyvanınız adi vaxtdan daha çox ayaqlarınıza sürtünür və ya səslər çıxarırsa, yemək vaxtı gəlmiş ola bilər.\n- Düzgün yemləmə qrafiki: Heyvanları gün ərzində eyni saatlarda yemləmək onların həzm sistemini tənzimləyir.\n- Porsiyanın çəkiyə uyğunluğu: Yem qablaşdırmasındakı cədvələ uyğun olaraq çəkiyə və yaşa uyğun qrammatura təyin edilməlidir. Təbii və quru yemlərin balansı mütləq qorunmalıdır.", new DateTime(2025, 2, 21, 12, 30, 0, 0, DateTimeKind.Utc), "/images/blog2.jpg", "how-to-know-your-pet-is-hungry", "Ev heyvanınızın ac olduğunu necə anlaya bilərsiniz? Qidalanma rejimi, aclıq siqnalları və düzgün porsiya seçimi haqqında əsas məsləhətlər.", "How to know your pet is hungry" },
                    { 3, "Murad Əliyev", "Evdə heyvan saxlamaq böyük sevinc bəxş etməklə yanaşı, onların təhlükəsizliyi üçün evdə xüsusi şərait qurmağı tələb edir.\n\n- Rahat yataq və istirahət guşəsi: Hər bir heyvanın özünə aid sakit, cərəyan olmayan bir guşəsi olmalıdır.\n- Təhlükəli əşyaların uzaqlaşdırılması: Açıqda qalan elektrik naqilləri, zəhərli otaq bitkiləri və kiçik oyuncaqlar yığışdırılmalıdır.\n- Əyləncə və fiziki aktivlik: Pişiklər üçün cırmaqlama taxtası, itlər üçün diş qaşıyan oyuncaqlar onların enerjisini düzgün istiqamətləndirir.\n- Daimi təmiz su mənbəyi: Su qabları həmişə təmiz və dolu saxlanılmalıdır.", new DateTime(2025, 2, 22, 15, 0, 0, 0, DateTimeKind.Utc), "/images/blog3.jpg", "best-home-for-your-pets", "Ev şəraitində heyvanlar üçün rahat, təhlükəsiz və əyləncəli mühitin yaradılması üzrə bələdçi.", "Best home for your pets" },
                    { 4, "Veterinar Elmir", "Düzgün qidalanma ev heyvanlarının parlaq tük örtüyü, möhkəm dişləri və güclü immuniteti üçün vacibdir.\n\n1. Premium yemlərin üstünlükləri: Yüksək keyfiyyətli quru yemlər təbii ət mənbələri, zəruri minerallar və vitaminlərlə zəngindir.\n2. Yaş həddinə görə ayrılma: Balalar (Puppy/Kitten) üçün yüksək kalorili və kalsiumlu yemlər, yetkin heyvanlar üçün isə balanslı rasion tələb olunur.\n3. İnsan yeməklərinin zərərləri: Şokolad, soğan, sarımsaq, şirniyyat və ədviyyatlı yeməklər heyvanların qaraciyər və mədəsinə ciddi zərər vura bilər.\n4. Mövsümi vitamin əlavələri: Xüsusilə tük tökülməsi dövründə baytar məsləhəti ilə Omega-3 və multivitaminlər qəbul edilməlidir.", new DateTime(2025, 3, 1, 9, 15, 0, 0, DateTimeKind.Utc), "/images/blog1.jpg", "ev-heyvanlari-ucun-duzgun-qidalanma-qaydalari", "İt və pişiklərin yaş və çəkisinə uyğun düzgün rasion seçilməsi və vitamin balansının qorunması haqqında məlumat.", "Ev heyvanları üçün düzgün qidalanma qaydaları" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts");
        }
    }
}
