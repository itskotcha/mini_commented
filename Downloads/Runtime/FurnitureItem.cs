// ==== FurnitureItem.cs ====
// คอมโพเนนต์ติดป้ายบน GameObject เฟอร์นิเจอร์ทุกชิ้นที่วางในห้อง เพื่อบอกว่า 'ชิ้นนี้คือเฟอร์นิเจอร์ชนิดไหน'
// นำเข้า UnityEngine — ใช้ MonoBehaviour
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส FurnitureItem (MonoBehaviour) — ข้อมูลประจำชิ้น
    // Id = ลำดับเฟอร์นิเจอร์พื้นฐานใน FurnitureCatalog (ใช้เมื่อ AssetKey ว่าง)
    // AssetKey = รหัส Key ของ asset ใน ApartmentLibrary (ถ้าไม่ว่าง แปลว่าชิ้นนี้มาจากคลัง ไม่ใช่เฟอร์นิเจอร์พื้นฐาน)
    public sealed class FurnitureItem : MonoBehaviour { public int Id; public string AssetKey; }
}
