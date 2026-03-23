using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;

namespace STAJ2.Seed
{
    public static class PermissionSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // EndpointPermissions tablosunu veritabanından kaldırdığımız ve 
            // Enum tabanlı merkezi Registry sistemine geçtiğimiz için 
            // buradaki eski kayıt (seed) işlemlerini temizledik.

            // Eğer ileride sisteminize varsayılan olarak eklenecek başka kayıtlar 
            // (örneğin standart Roller vb.) olursa burayı kullanabilirsiniz.
        }
    }
}