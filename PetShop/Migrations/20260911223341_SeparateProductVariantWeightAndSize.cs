using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetShop.Migrations
{
    /// <inheritdoc />
    public partial class SeparateProductVariantWeightAndSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Move weight strings mistakenly saved in Size into Weight
            migrationBuilder.Sql(@"
                UPDATE pv
                SET pv.Weight = pv.Size, pv.Size = NULL
                FROM ProductVariants pv
                WHERE (pv.Size LIKE '%kg%' OR pv.Size LIKE '%g%' OR pv.Size LIKE '%kq%' OR pv.Size LIKE '%qr%')
                  AND (pv.Weight IS NULL OR pv.Weight = '');

                -- 2. Move clothing size strings mistakenly saved in Weight into Size
                UPDATE pv
                SET pv.Size = pv.Weight, pv.Weight = NULL
                FROM ProductVariants pv
                WHERE pv.Weight IN ('XS', 'S', 'M', 'L', 'XL', 'XXL', 'XXXL')
                  AND (pv.Size IS NULL OR pv.Size = '');

                -- 3. If both are filled, resolve based on product category
                UPDATE pv
                SET pv.Size = NULL
                FROM ProductVariants pv
                INNER JOIN Products p ON pv.ProductId = p.Id
                LEFT JOIN ProductCategories c ON p.ProductCategoryId = c.Id
                LEFT JOIN ProductCategories pc ON c.ParentCategoryId = pc.Id
                WHERE pv.Weight IS NOT NULL AND pv.Size IS NOT NULL
                  AND (c.Slug LIKE 'food%' OR c.Name LIKE '%Yem%' OR pc.Slug LIKE 'food%' OR pc.Name LIKE '%Yem%');

                UPDATE pv
                SET pv.Weight = NULL
                FROM ProductVariants pv
                INNER JOIN Products p ON pv.ProductId = p.Id
                LEFT JOIN ProductCategories c ON p.ProductCategoryId = c.Id
                LEFT JOIN ProductCategories pc ON c.ParentCategoryId = pc.Id
                WHERE pv.Weight IS NOT NULL AND pv.Size IS NOT NULL
                  AND (c.Slug LIKE 'cloth%' OR c.Name LIKE '%Geyim%' OR pc.Slug LIKE 'cloth%' OR pc.Name LIKE '%Geyim%');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
