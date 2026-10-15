// ==== RoomObstacle.cs ====
// คลาส 'ป้ายกำกับ' (marker) ว่างเปล่า ใช้ติดบนกำแพง/หน้าต่าง/ประตู เพื่อให้โค้ดอื่นเช็คได้ว่า 'ชิ้นนี้เป็นสิ่งกีดขวาง'
// เช็คโดย GetComponent<RoomObstacle>() ใน RoomSandbox.IsValid, SandboxInput.CanStand/Occluded, PlanDoor.Blocked
// นำเข้า UnityEngine — ใช้ MonoBehaviour
using UnityEngine;
// ประกาศคลาส RoomObstacle แบบไม่มีเนื้อหา — มีไว้เพื่อเป็นป้ายอย่างเดียว
namespace HomeSandbox { public sealed class RoomObstacle : MonoBehaviour { } }
