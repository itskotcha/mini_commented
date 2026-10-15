// ==== SandboxMap.cs ====
// ข้อมูลของ 'แผนที่/ห้อง' หนึ่งแผนที่ (เช่น คอนโดโมเดิร์น หรือแปลนที่นำเข้า)
// StartScreen เก็บอาร์เรย์ของ SandboxMap และสลับเปิด/ปิดตามที่ผู้เล่นเลือก
// นำเข้า UnityEngine — ใช้ MonoBehaviour, Vector2/3, Transform, Texture2D, Quaternion
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส SandboxMap (MonoBehaviour) ติดบน GameObject ราก (root) ของแผนที่แต่ละอัน
    public sealed class SandboxMap : MonoBehaviour
    {
        // Id = รหัสเฉพาะของแผนที่ (ใช้ตั้งชื่อไฟล์เซฟ), Title = ชื่อที่แสดง, Description = คำอธิบายสั้นในหน้าเลือกแผนที่
        public string Id, Title, Description;
        // HalfSize = ครึ่งหนึ่งของขนาดห้อง (x = กว้าง/2, y = ลึก/2) หน่วยเมตร ใช้จำกัดขอบเขตวางของและเดิน
        public Vector2 HalfSize;
        // Spawn = ตำแหน่งที่ผู้เล่นไปเกิดเมื่อเข้าแผนที่ครั้งแรก
        public Vector3 Spawn;
        // Furniture = Transform แม่ที่เก็บเฟอร์นิเจอร์ทั้งหมดของแผนที่นี้
        public Transform Furniture;
        // Preview = รูปตัวอย่างที่แสดงในหน้าเลือกแผนที่
        public Texture2D Preview;
        // WalkBoundary = รายการจุดมุมของขอบเขตที่เดินได้ (convex hull) — [NonSerialized] คือไม่ให้ Unity บันทึกค่านี้ลง asset/scene
        [System.NonSerialized] public System.Collections.Generic.List<Vector2> WalkBoundary;
        // Visited = เคยเข้าแผนที่นี้ในรอบการเล่นนี้แล้วหรือยัง (ไม่บันทึกลงไฟล์)
        [System.NonSerialized] public bool Visited;
        // ReturnPosition = ตำแหน่งผู้เล่นตอนออกจากแผนที่ ไว้กลับมายืนที่เดิม
        [System.NonSerialized] public Vector3 ReturnPosition;
        // ReturnRotation = การหมุนของผู้เล่นตอนออก (ค่าเริ่มต้น Quaternion.identity = ไม่หมุน)
        [System.NonSerialized] public Quaternion ReturnRotation=Quaternion.identity;
    }
}
