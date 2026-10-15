// ==== PlanDoor.cs ====
// ประตูบานเปิด/ปิดได้ ที่ถูกสร้างขึ้นจากแปลนที่นำเข้า (เฉพาะประตูที่ผู้ใช้กดยืนยัน)
// Build() ประกอบวงกบ + บานประตู + มือจับ; Toggle() สั่งเปิด/ปิด; Update() หมุนบานทีละน้อย และหยุดถ้าชนผู้เล่น/ของ
// นำเข้า UnityEngine
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส PlanDoor (MonoBehaviour) ติดบน GameObject ราก 'Door' ของแต่ละบาน
    public sealed class PlanDoor : MonoBehaviour
    {
        // pivot = Transform ของจุดบานพับ (หมุนรอบแกนตั้งที่จุดนี้)
        Transform pivot;
        // leaf = BoxCollider ของบานประตู ใช้ตรวจการชน
        BoxCollider leaf;
        // player = ตัวควบคุมผู้เล่น (รู้ตำแหน่งตัวผู้เล่นและส่งข้อความได้)
        SandboxInput player;
        // angle = มุมปัจจุบันของบาน, target = มุมเป้าหมาย, swing = มุมเปิดสุด (องศา)
        float angle,target,swing;
        // hits = บัฟเฟอร์ Collider 32 ช่อง ใช้กับ OverlapBoxNonAlloc (ไม่สร้างขยะหน่วยความจำทุกเฟรม)
        readonly Collider[] hits=new Collider[32];
        // Toggle(): ถ้ากำลังเปิด (|target|>0.1) ให้ตั้งเป้าเป็น 0 (ปิด) ไม่งั้นตั้งเป้าเป็น swing (เปิด)
        public void Toggle() { target=Mathf.Abs(target)>.1f?0:swing; }
        // Build(...): ฟังก์ชัน static สร้างประตูทั้งชุด รับ root, ข้อมูลประตู, วัสดุบาน (wood), วัสดุวงกบ (frame), ตัวผู้เล่น
        public static void Build(GameObject root,PlanDoorData data,Material wood,Material frame,SandboxInput player)
        {
            // สร้าง GameObject 'Door / click or trigger to open' เป็นลูกของ root
            var go=new GameObject("Door / click or trigger to open");go.transform.SetParent(root.transform,false);
            // h = จุดบานพับบนแปลน (Vector2: x→แกน X, y→แกน Z ของโลก 3 มิติ) วางประตูที่จุดนั้น
            // แล้วหมุนด้วย ClosedYaw ให้แกน X ของประตูวิ่งตามแนวช่องเปิด
            var h=data.Hinge;go.transform.localPosition=new Vector3(h.x,0,h.y);go.transform.localRotation=Quaternion.Euler(0,data.ClosedYaw,0);
            // ติด PlanDoor ให้ประตู เก็บตัวผู้เล่นและมุมเปิด (SwingYaw)
            var door=go.AddComponent<PlanDoor>();door.player=player;door.swing=data.SwingYaw;
            // length = ความกว้างช่องใช้งานจริง (PassageWidth)
            float length=data.PassageWidth;
            // ถ้าช่องเปิดในแปลนกว้างกว่าช่องประตู
            if(data.Length>length+.001f)
                // สร้างกำแพงเติมส่วนเกินข้างประตู (กว้างเท่าส่วนที่เกิน สูง 2.8 ม.)
                Part(go,"Wall beside door",new Vector3((length+data.Length)/2,1.4f,0),new Vector3(data.Length-length,2.8f,.12f),frame);
            // เสากรอบฝั่งบานพับ (Hinge jamb) กว้าง 5 ซม. สูง 2.2 ม.
            Part(go,"Hinge jamb",new Vector3(.025f,1.1f,0),new Vector3(.05f,2.2f,.14f),frame);
            // เสากรอบฝั่งลิ้นล็อก (Latch jamb) ที่ปลายอีกด้าน
            Part(go,"Latch jamb",new Vector3(length-.025f,1.1f,0),new Vector3(.05f,2.2f,.14f),frame);
            // คานบนวงกบ (Header) ที่ความสูง 2.16 ม.
            Part(go,"Header",new Vector3(length/2,2.16f,0),new Vector3(length,.08f,.14f),frame);
            // กำแพงเหนือประตู (Above door) ช่วงสูง 2.2–2.8 ม.
            Part(go,"Above door",new Vector3(length/2,2.5f,0),new Vector3(length,.6f,.12f),frame);
            // สร้างจุดหมุน 'Hinge' เป็นลูกของประตู เลื่อนเข้ามา 5.5 ซม. (ชิดวงกบ) แล้วเก็บลง door.pivot
            door.pivot=new GameObject("Hinge").transform;door.pivot.SetParent(go.transform,false);door.pivot.localPosition=new Vector3(.055f,0,0);
            // สร้างบานประตู (Door leaf) ลูกของ pivot: ยาว length-0.11, สูง 2.1, หนา 4.5 ซม. (ตั้งกึ่งกลางที่ครึ่งความยาวให้ขอบซ้ายอยู่ที่จุดหมุนพอดี)
            var leaf=Part(door.pivot.gameObject,"Door leaf",new Vector3((length-.11f)/2,1.07f,0),new Vector3(length-.11f,2.1f,.045f),wood);
            // เก็บ BoxCollider ของบาน ไว้ใช้ตรวจชน
            door.leaf=leaf.GetComponent<BoxCollider>();
            // สร้างมือจับ (Handle) เป็นลูกของ pivot (หมุนไปกับบาน) ที่ปลายด้านลิ้นล็อก
            var handle=FurnitureCatalog.Part(door.pivot.gameObject,"Handle",new Vector3(length-.19f,1.02f,-.06f),new Vector3(.1f,.025f,.09f),frame);
            // ปิด Collider ของมือจับ (ไม่ให้ขวางผู้เล่นหรือการคลิก)
            handle.GetComponent<Collider>().enabled=false;
        }
        // Part(...): ฟังก์ชันช่วยแบบ private ของคลาสนี้ — สร้างกล่องแล้วทำให้เป็นสิ่งกีดขวางจริง
        static GameObject Part(GameObject parent,string name,Vector3 p,Vector3 size,Material material)
        // สร้างด้วย FurnitureCatalog.Part, เปิด Collider, ติด RoomObstacle แล้วคืน GameObject
        { var go=FurnitureCatalog.Part(parent,name,p,size,material);go.GetComponent<Collider>().enabled=true;go.AddComponent<RoomObstacle>();return go; }
        // Update(): ทำงานทุกเฟรม เพื่อหมุนบานเข้าหามุมเป้าหมาย
        void Update()
        {
            // ถ้าบานอยู่ใกล้มุมเป้าหมายแล้ว (ห่าง < 0.01°) ไม่ต้องทำอะไร
            if(Mathf.Abs(angle-target)<.01f) return;
            // หมายเหตุเดิม: แบ่งหมุนเป็นขั้นย่อยเล็กๆ กันไม่ให้เฟรมกระตุกแล้วบานทะลุคน/ของที่อยู่ใกล้
            // Small angular substeps prevent a fast frame from skipping a nearby person/object.
            // next = มุมถัดไปของเฟรมนี้: ขยับเข้าหาเป้าหมายด้วยความเร็ว 100°/วินาที โดยจำกัดเวลาต่อเฟรมไม่เกิน 0.05 วินาที
            float next=Mathf.MoveTowards(angle,target,100*Mathf.Min(Time.deltaTime,.05f));
            // steps = จำนวนขั้นย่อย ≈ 1 ขั้นต่อ 1 องศา (อย่างน้อย 1)
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(next-angle)));
            // increment = มุมที่เพิ่มในแต่ละขั้นย่อย
            float increment=(next-angle)/steps;
            // วนทุกขั้นย่อย
            for(int s=0;s<steps;s++)
            {
                // ทดลองหมุน pivot ไปที่มุม (angle + increment)
                pivot.localRotation=Quaternion.Euler(0,angle+increment,0);
                // ถ้าชน (Blocked): คืนบานกลับมุมเดิม, ตั้งเป้าหมายเท่ามุมปัจจุบัน (หยุดบาน),
                // แจ้งข้อความ 'Door blocked...' ให้ผู้เล่น แล้วออกจาก Update
                if(Blocked()) { pivot.localRotation=Quaternion.Euler(0,angle,0);target=angle;player.Room.Message("Door blocked. Move clear and click again.");return; }
                // ไม่ชน: ยอมรับมุมใหม่ (angle += increment)
                angle+=increment;
            }
        }
        // Blocked(): ตรวจว่าบานในตำแหน่งปัจจุบันชนผู้เล่นหรือของในห้องหรือไม่
        bool Blocked()
        {
            // แปลงตำแหน่งกล้องผู้เล่นเป็นพิกัดโลคอลของบานประตู
            var local=leaf.transform.InverseTransformPoint(player.View.transform.position);
            // ถ้าผู้เล่นอยู่ในกรอบของบาน (ครึ่งหนึ่งของลูกบาศก์ ±0.5 + ระยะเผื่อ 0.3 ม. ที่แปลงเป็นหน่วยโลคอลด้วย lossyScale)
            // ทั้งแกน x และ z → ถือว่าชน
            if(Mathf.Abs(local.x)<.5f+.3f/leaf.transform.lossyScale.x && Mathf.Abs(local.z)<.5f+.3f/leaf.transform.lossyScale.z) return true;
            // half = ครึ่งขนาดจริงของกล่องบาน (ขนาด × สเกล) คูณ 0.48 ให้เล็กกว่าจริงนิดหน่อย กันชนผิวสัมผัส
            Vector3 half=Vector3.Scale(leaf.size,leaf.transform.lossyScale)*.48f;
            // ตรวจกล่องเดียวกับบาน (ตำแหน่งกลางกล่อง, หมุนตามบาน, ทุกเลเยอร์, ไม่นับ trigger) เก็บ Collider ที่ซ้อนลง hits และได้จำนวน count
            int count=Physics.OverlapBoxNonAlloc(leaf.transform.TransformPoint(leaf.center),half,hits,leaf.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            // ถ้าบัฟเฟอร์เต็ม (เจอเยอะผิดปกติ) ถือว่าชนไว้ก่อนเพื่อความปลอดภัย
            if(count==hits.Length) return true;
            // วนตรวจ: ถ้าไม่ใช่ส่วนของประตูนี้เอง และ (เป็น RoomObstacle หรือเป็น/อยู่ใต้ FurnitureItem) → ชน
            for(int i=0;i<count;i++) if(!hits[i].transform.IsChildOf(transform) && (hits[i].GetComponent<RoomObstacle>() || hits[i].GetComponentInParent<FurnitureItem>())) return true;
            // ไม่ชนอะไรเลย คืน false
            return false;
        }
    }
}
