// ==== ApartmentLibrary.cs ====
// คลังข้อมูล (ScriptableObject) เก็บรายการเฟอร์นิเจอร์/โมเดลสำเร็จรูปของอพาร์ตเมนต์ ที่ผู้เล่นเลือกวางได้
// ถูกใช้โดย: AssetMenu (แสดงรายการ), RoomSandbox (สร้างชิ้น/คิดราคา), LayoutStore (ตรวจว่า asset ยังมีอยู่ตอนโหลดเลย์เอาต์)
// นำเข้า namespace System — ใช้ [Serializable] และ Array.Empty<T>()
using System;
// นำเข้า UnityEngine — ใช้ ScriptableObject, GameObject, [CreateAssetMenu], [Min]
using UnityEngine;
// กำหนด namespace HomeSandbox ให้ทุกคลาสในโปรเจกต์นี้อยู่ในขอบเขตชื่อเดียวกัน
namespace HomeSandbox
{
    // เพิ่มเมนูใน Unity Editor (Assets > Create > Home Sandbox > Apartment Library) เพื่อสร้างไฟล์ asset ของคลังนี้
    [CreateAssetMenu(menuName="Home Sandbox/Apartment Library")]
    // ประกาศคลาส ApartmentLibrary สืบทอดจาก ScriptableObject = ก้อนข้อมูลที่เก็บเป็นไฟล์ asset ไม่ต้องแปะบน GameObject
    // sealed = ห้ามมีคลาสอื่นสืบทอดต่อ
    public sealed class ApartmentLibrary : ScriptableObject
    {
        // คลาสย่อย Entry แทนรายการ 1 ชิ้นในคลัง ([Serializable] ทำให้ Unity เก็บและโชว์ใน Inspector ได้)
        // ฟิลด์: Key = รหัสเฉพาะของ asset (ใช้อ้างอิง/บันทึกเซฟ), Name = ชื่อที่แสดง, Category = หมวดหมู่ (ใช้ค้นหา)
        // Prefab = โมเดล 3 มิติที่จะสร้างจริง, HasPrice = มีราคากำหนดหรือไม่, PriceTHB = ราคา (บาท) โดย [Min(0)] ไม่ให้ติดลบ
        [Serializable] public class Entry { public string Key, Name, Category; public GameObject Prefab; public bool HasPrice; [Min(0)] public int PriceTHB; }
        // อาร์เรย์รายการทั้งหมดในคลัง เริ่มต้นเป็นอาร์เรย์ว่าง (ไม่ใช่ null) เพื่อกัน NullReference
        public Entry[] Entries=Array.Empty<Entry>();
        // Find(key): วนหา Entry ที่ Key ตรงกับที่ขอ แล้วคืนตัวแรกที่เจอ
        // ถ้าวนจนหมดแล้วไม่เจอ คืน null
        public Entry Find(string key) { foreach(var entry in Entries) if(entry.Key==key) return entry; return null; }
    }
}
