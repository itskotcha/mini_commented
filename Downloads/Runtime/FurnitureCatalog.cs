// ==== FurnitureCatalog.cs ====
// รายการเฟอร์นิเจอร์ 'พื้นฐาน' 10 ชิ้นที่สร้างจากกล่อง (cube) ในโค้ดล้วนๆ ไม่ต้องใช้โมเดลภายนอก พร้อมชื่อ ขนาด ราคา
// และฟังก์ชัน Part() ที่ไฟล์อื่น (ประตู, หน้าต่าง, กำแพง) ใช้สร้างชิ้นส่วนกล่องร่วมกัน
// นำเข้า UnityEngine
using UnityEngine;

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส static รวมข้อมูลและฟังก์ชันสร้างเฟอร์นิเจอร์พื้นฐาน
    public static class FurnitureCatalog
    {
        // Names: ชื่อเฟอร์นิเจอร์ 10 ชิ้น ลำดับในอาร์เรย์ = Id (0 โซฟา, 1 เก้าอี้นวม, 2 โต๊ะกาแฟ, 3 โต๊ะอาหาร, 4 เก้าอี้ทานข้าว, 5 เตียง, 6 ตู้เสื้อผ้า, 7 โต๊ะทำงาน, 8 ตู้เตี้ย, 9 แท่นต้นไม้)
        public static readonly string[] Names = { "Linen sofa", "Lounge chair", "Coffee table", "Dining table", "Dining chair", "Queen bed", "Wardrobe", "Writing desk", "Low cabinet", "Plant stand" };
        // Sizes: ขนาดของแต่ละชิ้น (x = กว้าง, y = สูง, z = ลึก) หน่วยเมตร เรียงตาม Id
        public static readonly Vector3[] Sizes = { new Vector3(2.2f,.9f,.95f), new Vector3(.85f,.9f,.85f), new Vector3(1.1f,.42f,.6f), new Vector3(1.5f,.76f,.85f), new Vector3(.5f,.9f,.55f), new Vector3(1.65f,.85f,2.1f), new Vector3(1.5f,2.1f,.6f), new Vector3(1.3f,.76f,.65f), new Vector3(1.8f,.65f,.45f), new Vector3(.45f,1.1f,.45f) };
        // Prices: ราคาตัวอย่างของแต่ละชิ้น หน่วยบาท เรียงตาม Id
        public static readonly int[] Prices = { 18900,6900,2900,5900,1900,15900,12900,4900,6900,1200 };

        // Create(id,parent,wood,fabric,dark): ประกอบเฟอร์นิเจอร์ชนิด id จากกล่องหลายชิ้น คืน GameObject ราก; wood/fabric/dark = วัสดุสามแบบ
        public static GameObject Create(int id, Transform parent, Material wood, Material fabric, Material dark)
        {
            // สร้าง GameObject รากชื่อตามชนิด เป็นลูกของ parent
            var root = new GameObject(Names[id]); root.transform.SetParent(parent, false);
            // size = ขนาดโดยรวมของชนิดนี้
            var size = Sizes[id];
            // id 0,1 (โซฟา/เก้าอี้นวม):
            if (id == 0 || id == 1)
            {
                // ที่นั่ง (Seat) ใช้วัสดุผ้า
                Part(root,"Seat",new Vector3(0,.4f,0),new Vector3(size.x,.25f,size.z),fabric);
                // พนักพิง (Back) อยู่ด้านหลัง (z บวก) ที่ 40% ของความลึก
                Part(root,"Back",new Vector3(0,.72f,size.z*.4f),new Vector3(size.x,.36f,.18f),fabric);
                // ที่วางแขนสองข้าง (sign = -1 และ +1 ซ้าย/ขวา)
                foreach (float sign in new[]{-1f,1f}) Part(root,"Arm",new Vector3(sign*(size.x/2-.08f),.57f,0),new Vector3(.16f,.25f,size.z),fabric);
                // ขา 4 ขา (ฟังก์ชัน Legs) สูง 0.28 ม. วัสดุไม้
                Legs(root,size.x,size.z,.28f,wood);
            }
            // id 2,3,7 (โต๊ะกาแฟ/โต๊ะอาหาร/โต๊ะทำงาน):
            else if (id == 2 || id == 3 || id == 7)
            {
                // แผ่นท็อปหนา 8 ซม. วางที่ความสูงสุดของชิ้น
                Part(root,"Top",new Vector3(0,size.y-.04f,0),new Vector3(size.x,.08f,size.z),wood);
                // ขา 4 ขา สูงเท่าความสูงโต๊ะลบความหนาท็อป วัสดุสีเข้ม
                Legs(root,size.x,size.z,size.y-.08f,dark);
            }
            // id 4 (เก้าอี้ทานข้าว):
            else if (id == 4)
            {
                // ที่นั่ง
                Part(root,"Seat",new Vector3(0,.45f,0),new Vector3(.5f,.1f,.5f),fabric);
                // พนักพิง วัสดุไม้
                Part(root,"Back",new Vector3(0,.7f,.22f),new Vector3(.5f,.4f,.06f),wood);
                // ขา 4 ขา สูง 0.4 ม.
                Legs(root,.5f,.5f,.4f,wood);
            }
            // id 5 (เตียง):
            else if (id == 5)
            {
                // โครงเตียง (Frame)
                Part(root,"Frame",new Vector3(0,.22f,0),new Vector3(size.x,.32f,size.z),wood);
                // ที่นอน (Mattress) วางเหนือโครง
                Part(root,"Mattress",new Vector3(0,.45f,-.04f),new Vector3(1.55f,.22f,1.95f),fabric);
                // หัวเตียง (Headboard) ด้านหลัง
                Part(root,"Headboard",new Vector3(0,.5f,.99f),new Vector3(size.x,.7f,.12f),wood);
                // หมอน 2 ใบ (p=0 ซ้าย, p=1 ขวา)
                for(int p=0;p<2;p++) Part(root,"Pillow",new Vector3(p==0?-.4f:.4f,.6f,.65f),new Vector3(.6f,.12f,.4f),fabric);
            }
            // id 9 (แท่นต้นไม้):
            else if(id == 9)
            {
                // กระถาง (Pot)
                Part(root,"Pot",new Vector3(0,.22f,0),new Vector3(.32f,.44f,.32f),fabric);
                // ลำต้น (Stem) ทรงกล่องบางสูง
                Part(root,"Stem",new Vector3(0,.6f,0),new Vector3(.05f,.6f,.05f),wood);
                // พุ่มใบ (Foliage) เป็นทรงกลม (PrimitiveType.Sphere) วัสดุสีเข้ม
                Part(root,"Foliage",new Vector3(0,.87f,0),new Vector3(.45f,.45f,.45f),dark,PrimitiveType.Sphere);
            }
            // กรณีอื่น (id 6 ตู้เสื้อผ้า และ id 8 ตู้เตี้ย):
            else
            {
                // ตัวตู้ (Cabinet) ขนาดเท่า size วางตั้งบนพื้น
                Part(root,"Cabinet",new Vector3(0,size.y/2,0),size,wood);
                // บานประตูตู้ 2 บาน อยู่ด้านหน้า (z ติดลบ) ฝั่งซ้ายและขวา กว้างบานละ 48% ของตู้
                for(int p=0;p<2;p++) Part(root,"Door",new Vector3((p==0?-.25f:.25f)*size.x,size.y/2,-size.z/2-.006f),new Vector3(size.x*.48f,size.y*.94f,.015f),fabric);
            }
            // เพิ่ม BoxCollider ครอบทั้งชิ้น (center ครึ่งความสูง, size = ขนาดชิ้น) ใช้ชนและคลิกเลือก
            var collider = root.AddComponent<BoxCollider>(); collider.center = Vector3.up*size.y/2; collider.size = size;
            // ติด FurnitureItem และตั้ง Id
            var item = root.AddComponent<FurnitureItem>(); item.Id = id;
            // คืน GameObject ราก
            return root;
        }

        // Legs(root,x,z,h,mat): สร้างขา 4 ขาที่มุมทั้งสี่ ของชิ้นกว้าง x ลึก z สูง h
        static void Legs(GameObject root,float x,float z,float h,Material mat)
        {
            // วน a และ b ใน {-1,+1} = ได้ 4 มุม
            foreach(float a in new[]{-1f,1f}) foreach(float b in new[]{-1f,1f})
                // สร้างขา 6.5 ซม. ห่างจากขอบ 9 ซม. สูง h (ตั้งกึ่งกลางที่ h/2)
                Part(root,"Leg",new Vector3(a*(x/2-.09f),h/2,b*(z/2-.09f)),new Vector3(.065f,h,.065f),mat);
        }
        // Part(...): สร้างชิ้นส่วนกล่อง (หรือทรงอื่น) 1 ชิ้น — ใช้ร่วมกับ PlanDoor, FloorPlanWindows, FloorPlanImport
        public static GameObject Part(GameObject root,string name,Vector3 position,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube)
        {
            // สร้าง primitive ตาม type (ค่าเริ่มต้น Cube), ตั้งชื่อ, ผูกเป็นลูกของ root
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(root.transform,false);
            // ตั้งตำแหน่งโลคอล, สเกล (ใช้สเกลเป็นขนาด), และวัสดุ
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
            // ปิด Collider ไว้ก่อนเสมอ (ผู้เรียกจะเปิดเองเมื่อต้องการ)
            var collider = go.GetComponent<Collider>(); collider.enabled = false;
            // คืน GameObject ที่สร้าง
            return go;
        }
    }
}
