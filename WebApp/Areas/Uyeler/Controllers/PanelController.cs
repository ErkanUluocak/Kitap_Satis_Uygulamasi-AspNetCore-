using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Areas.Uyeler.Controllers
{
    [Area("Uyeler")]
    public class PanelController : Controller
    {
        UserManager<Uye> _userManager;
        ApplicationDbContext _db;

        public PanelController(ApplicationDbContext db, UserManager<Uye> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            int uyeID = int.Parse(_userManager.GetUserId(User));
            var result = _db.Sepetler.Include("Kitap").Where(s => s.UyeID == uyeID).ToList();
            return View(result);
        }

        public IActionResult SepeteEkle(int id)
        {
            //yoksa ekle
            //varsa => güncelle sepetin sayısını artır

            int uyeID = int.Parse(_userManager.GetUserId(User));

            if (_db.Sepetler.Where(s => s.KitapID == id && s.UyeID == uyeID).Count() > 0)
            {
                //Update
                Sepet sepet = _db.Sepetler.Where(s => s.KitapID == id && s.UyeID == uyeID).Single();
                sepet.Adet += 1;
                _db.Entry<Sepet>(sepet).State = EntityState.Modified;
            }
            else
            {
                //Insert
                Sepet sepet = new Sepet
                {
                    KitapID = id,
                    UyeID = uyeID,
                    Adet = 1
                };
                _db.Sepetler.Add(sepet);
            }
            _db.SaveChanges();
            return Redirect("~/Kitap/Index"); //sepete eklediğinde sepete ekliyor ama aynı sayfada kalıyor.
        }

        public IActionResult SepettenCikar(int id)
        {
            int uyeID = int.Parse(_userManager.GetUserId(User));
            Sepet sepet = _db.Sepetler.Find(id);
            if (sepet.Adet > 1)
            {
                //update
                sepet.Adet -= 1;
                _db.Entry(sepet).State = EntityState.Modified;
            }
            else if (true)
            {
                //delete
                _db.Sepetler.Remove(sepet);
            }
            _db.SaveChanges();
            return Redirect("~/Uyeler/Panel/Index");
        }

        public IActionResult SepetiBosalt(int id)
        {
            int uyeID = int.Parse(_userManager.GetUserId(User));
            SepetiTumunuTemizle(uyeID);
            return Redirect("~/Uyeler/Panel/Index");
        }

        private void SepetiTumunuTemizle(int uyeID)
        {
            var sonuc = _db.Sepetler.Where(s => s.UyeID == uyeID).ToList();
            _db.Sepetler.RemoveRange(sonuc); //tümünü silmek için
            _db.SaveChanges();
        }
        private bool StokKontrolu(int uyeID, out string detay, out decimal toplamTutar)
        {
            //true ise var
            bool kontrol = true;
            detay = "";
            var sonuc = _db.Sepetler.Include("Kitap").Where(s => s.UyeID == uyeID).ToList();
            toplamTutar = 0;
            foreach (Sepet sepet in sonuc)
            {
                if (sepet.Adet > sepet.Kitap.StokAdedi)
                {
                    kontrol = false;
                    detay += sepet.Kitap.KitapAdi + " " + sepet.Kitap.StokAdedi + " ";                  
                }
                toplamTutar += sepet.Adet * sepet.Kitap.Fiyat;
            }
            return kontrol;
        }

        public IActionResult SatinAl(int id)
        {
            //1- Stok kontrolü
            //2- Satışa Kaydet
            //3- Detaya Kaydet
            //4- Stoktan düş
            //5- Sepeti Boşalt

            int uyeID = int.Parse(_userManager.GetUserId(User));

            if (StokKontrolu(uyeID, out string Mesaj, out decimal toplamTutar))
            {
                //2.Aşama
                Satis satis = new Satis()
                {
                    UyeID = uyeID,
                    SatisTarihi = DateTime.Now,
                    ToplamTutar = toplamTutar
                };

                _db.Satislar.Add(satis);
                _db.SaveChanges();

                int satisID = satis.SatisID;

                //3.Aşama
                var sepet = _db.Sepetler.Include("Kitap").Where(s => s.UyeID == uyeID).ToList();

                foreach (Sepet s in sepet)
                {
                    //dönerken satış detaya ekleme
                    SatisDetay detay = new SatisDetay
                    {
                        KitapID = s.KitapID,
                        SatisID = satisID,
                        Adet = s.Adet,
                        Fiyat = s.Kitap.Fiyat
                    };

                    _db.SatisDetaylari.Add(detay);

                    //stoktan düşme

                    s.Kitap.StokAdedi -= s.Adet;
                    _db.Entry(s.Kitap).State = EntityState.Modified;
                }

                _db.SaveChanges();
                SepetiTumunuTemizle(uyeID);

            }
            else
            {
                TempData["Mesaj"] = Mesaj;
            }

            return Redirect("~/Uyeler/Panel/Index");
        }
    }
}
