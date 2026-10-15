// ==== PlanWalkBoundary.cs ====
// คำนวณ 'ขอบเขตที่เดินได้' ของแปลนที่นำเข้า (เป็น convex hull ของมุมกำแพงทั้งหมด) และหาจุดเกิดที่ปลอดภัยของผู้เล่น
// ใช้โดย: FloorPlanImport.Install, StartScreen.Enter, SandboxInput.CanStand
// นำเข้า System.Collections.Generic — ใช้ List<T>
using System.Collections.Generic;
// นำเข้า UnityEngine — ใช้ Vector2/3, Rect, Mathf
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส static (ไม่ต้องสร้างอินสแตนซ์) รวมฟังก์ชันคำนวณขอบเขต
    public static class PlanWalkBoundary
    {
        // หมายเหตุเดิม: เป็นขอบเขตภายนอกแบบหยาบ ส่วนห้องย่อย/ซอกมุมยังอาศัย collider ของกำแพงกั้นอีกชั้น
        // Coarse outer safety boundary. Wall colliders still enforce recesses and rooms.
        // Build(walls): สร้าง convex hull จากรายการสี่เหลี่ยมกำแพง ด้วยอัลกอริทึม Monotone Chain (Andrew)
        // คืนรายการจุดมุมเรียงทวนเข็มนาฬิกา
        public static List<Vector2> Build(List<Rect> walls)
        {
            // สร้างลิสต์เก็บจุดทั้งหมด
            var points=new List<Vector2>();
            // วนทุกกำแพง แล้วใส่มุมทั้ง 4 (ซ้ายล่าง, ขวาล่าง, ขวาบน, ซ้ายบน) ลงในลิสต์
            foreach(var r in walls) { points.Add(new Vector2(r.xMin,r.yMin));points.Add(new Vector2(r.xMax,r.yMin));points.Add(new Vector2(r.xMax,r.yMax));points.Add(new Vector2(r.xMin,r.yMax)); }
            // เรียงจุดตามแกน x จากน้อยไปมาก ถ้า x เท่ากันให้เรียงตาม y
            points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            // ลิสต์ hull ที่จะเก็บผลลัพธ์
            var hull=new List<Vector2>();
            // ส่วนล่างของ hull: วนจุดจากซ้ายไปขวา — ถ้าการเลี้ยวไม่ใช่ทวนเข็ม (Cross<=0) ให้ลบจุดท้ายออกจนกว่าจะถูกต้อง
            // จากนั้นเพิ่มจุดปัจจุบันเข้า hull
            foreach(var p in points) { while(hull.Count>=2 && Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0) hull.RemoveAt(hull.Count-1);hull.Add(p); }
            // จำจำนวนจุดของส่วนล่างไว้ (lower) เพื่อไม่ให้ส่วนบนไปลบทับ
            int lower=hull.Count;
            // ส่วนบนของ hull: วนย้อนกลับจากขวาไปซ้าย ทำเหมือนส่วนล่าง (while ใช้ hull.Count>lower กันลบส่วนล่าง)
            for(int i=points.Count-2;i>=0;i--) { var p=points[i];while(hull.Count>lower && Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0) hull.RemoveAt(hull.Count-1);hull.Add(p); }
            // จุดสุดท้ายซ้ำกับจุดแรก จึงลบทิ้ง (เมื่อมีมากกว่า 1 จุด) แล้วคืน hull
            if(hull.Count>1) hull.RemoveAt(hull.Count-1);return hull;
        }
        // Cross(a,b,p): ผลคูณไขว้ 2 มิติของเวกเตอร์ a→b กับ a→p
        // ค่า > 0 = จุด p อยู่ทางซ้ายของเส้น a→b (เลี้ยวทวนเข็ม), = 0 อยู่บนเส้นเดียวกัน, < 0 อยู่ทางขวา
        static float Cross(Vector2 a,Vector2 b,Vector2 p) => (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
        // Contains(hull,point,radius): ตรวจว่าจุดอยู่ภายใน hull และห่างจากขอบทุกด้านอย่างน้อย radius หรือไม่
        public static bool Contains(List<Vector2> hull,Vector2 point,float radius)
        {
            // ถ้า hull เป็น null หรือมีน้อยกว่า 3 จุด (ไม่เป็นรูปปิด) ถือว่าไม่อยู่ข้างใน
            if(hull==null || hull.Count<3) return false;
            // วนทุกขอบ a→b ของ hull — Cross/ความยาวขอบ = ระยะตั้งฉากจากจุดถึงขอบ (เป็นบวกเมื่ออยู่ด้านใน)
            // ถ้าระยะน้อยกว่า radius แสดงว่าอยู่นอกหรือชิดขอบเกินไป → คืน false
            for(int i=0;i<hull.Count;i++) { var a=hull[i];var b=hull[(i+1)%hull.Count];if(Cross(a,b,point)<radius*(b-a).magnitude) return false; }
            // ผ่านทุกขอบแล้ว → จุดอยู่ภายในพอ คืน true
            return true;
        }
        // TrySpawn: หาจุดเกิดที่ดีที่สุดภายในแปลน ส่งผ่าน out spawn คืน true ถ้าหาเจอ
        public static bool TrySpawn(FloorPlanImport.Plan plan,List<Vector2> hull,out Vector3 spawn)
        {
            // ตั้งค่าเริ่มต้น spawn = (0,0,0) และ best = ลบอนันต์ (ยังไม่มีคะแนนที่ดีที่สุด)
            spawn=Vector3.zero;float best=float.NegativeInfinity;
            // ลูปนอก: สแกนแกน z (ความลึก) ทีละ 0.2 ม. เว้นขอบ 0.35 ม.
            for(float z=-plan.depth/2+.35f;z<plan.depth/2-.35f;z+=.2f)
            // ลูปใน: สแกนแกน x (ความกว้าง) ทีละ 0.2 ม. เว้นขอบ 0.35 ม. (ลูปนี้เป็นเนื้อหาของลูป z ด้านบน)
            for(float x=-plan.width/2+.35f;x<plan.width/2-.35f;x+=.2f)
            {
                // สร้างจุดทดสอบ p; ถ้าอยู่นอก hull (เผื่อรัศมีตัวผู้เล่น 0.35 ม.) ข้ามไป
                var p=new Vector2(x,z);if(!Contains(hull,p,.35f)) continue;
                // clearance = ระยะห่างต่ำสุดจากสิ่งกีดขวาง เริ่มที่อนันต์
                float clearance=float.PositiveInfinity;
                // หาระยะที่ใกล้ที่สุดไปยังกำแพงทุกตัว
                foreach(var r in plan.walls) clearance=Mathf.Min(clearance,Distance(r,p));
                // รวมระยะไปยังหน้าต่างด้วย (ถ้ามี)
                if(plan.windows!=null) foreach(var r in plan.windows) clearance=Mathf.Min(clearance,Distance(r,p));
                // รวมระยะไปยังช่องประตูที่ผู้ใช้ยืนยันแล้ว (door.confirmed) ด้วย
                if(plan.doors!=null) foreach(var door in plan.doors) if(door.confirmed) clearance=Mathf.Min(clearance,Distance(door.opening,p));
                // ถ้าใกล้สิ่งกีดขวางน้อยกว่า 0.4 ม. ข้ามจุดนี้
                if(clearance<.4f) continue;
                // หมายเหตุเดิม: เลือกตำแหน่งโล่งในตัวห้อง ไม่ใช่ขอบกระดาษว่างๆ ของภาพแปลน
                // Prefer a roomy interior location rather than the first empty page margin.
                // คะแนน = min(clearance,1.2) − 0.025×ระยะจากศูนย์กลางแปลน → ชอบที่โล่งและค่อนมาทางกลาง
                float score=Mathf.Min(clearance,1.2f)-p.magnitude*.025f;
                // ถ้าคะแนนดีกว่าที่ผ่านมา จำคะแนนและตำแหน่ง (x,0,z) ไว้
                if(score>best) { best=score;spawn=new Vector3(x,0,z); }
            }
            // ถ้า best ไม่ใช่ลบอนันต์ แสดงว่าเจอจุดเกิด → คืน true
            return !float.IsNegativeInfinity(best);
        }
        // Distance(r,p): ระยะจากจุด p ถึงสี่เหลี่ยม r (เป็น 0 ถ้า p อยู่ข้างใน)
        static float Distance(Rect r,Vector2 p)
        // dx,dy = ระยะส่วนที่ล้นออกนอกสี่เหลี่ยมในแต่ละแกน (หรือ 0) แล้วคืนความยาวเวกเตอร์ √(dx²+dy²)
        { float dx=Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),dy=Mathf.Max(r.yMin-p.y,0,p.y-r.yMax);return Mathf.Sqrt(dx*dx+dy*dy); }
    }
}
