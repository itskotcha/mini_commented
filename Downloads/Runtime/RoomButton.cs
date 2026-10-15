// ==== RoomButton.cs ====
// คอมโพเนนต์ของ 'ปุ่ม 3 มิติ' ในโลก VR — เมื่อผู้เล่นยิงเลเซอร์ใส่แล้วกด trigger จะส่งต่อไปที่ RoomSandbox.Command()
// นำเข้า UnityEngine — ใช้ MonoBehaviour
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส RoomButton (MonoBehaviour) ติดบน Cube ที่ทำหน้าที่เป็นปุ่ม
    public sealed class RoomButton : MonoBehaviour
    {
        // Command = รหัสคำสั่ง (ดูความหมายแต่ละเลขใน switch ของ RoomSandbox.Command เช่น 0=วางเฟอร์นิเจอร์ใหม่, 1=เซฟ, 2=โหลด)
        public int Command;
        // ItemId = พารามิเตอร์ประกอบคำสั่ง เช่น ลำดับเฟอร์นิเจอร์ / ลำดับแผนที่ / ทิศของการเปลี่ยนหน้า (-1 หรือ +1)
        public int ItemId;
    }
}
