// ==== StartScreen.cs ====
// หน้าเมนูหลัก / หน้าเลือกแผนที่ (แผนที่ = ห้องหนึ่งแบบ) ควบคุมการสลับระหว่าง 'ฉากต้อนรับ' กับ 'ภายในห้อง'
// และจำตำแหน่งผู้เล่นของแต่ละแผนที่ไว้ตอนออก/กลับเข้ามา วาดหน้าจอด้วย IMGUI (OnGUI) บนคอมพิวเตอร์
// นำเข้า UnityEngine
using UnityEngine;
// นำเข้า UnityEngine.XR — ใช้ XRSettings.isDeviceActive
using UnityEngine.XR;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส StartScreen (MonoBehaviour)
    public sealed class StartScreen : MonoBehaviour
    {
        // Interior = GameObject ของโลกภายในห้อง; Welcome = GameObject ของฉากต้อนรับ/เมนู (สลับเปิดทีละอัน)
        public GameObject Interior, Welcome;
        // Player = ตัวควบคุมผู้เล่น (SandboxInput) ใช้ย้ายตำแหน่งผู้เล่น
        public SandboxInput Player;
        // Maps = รายการแผนที่ทั้งหมดที่เลือกได้
        public SandboxMap[] Maps;
        // MapLabel = ข้อความ 3 มิติแสดงชื่อ/คำอธิบายแผนที่ที่เลือก (ใน VR)
        public TextMesh MapLabel;
        // SharedCatalog = Transform ของแผงแคตตาล็อกฝั่ง VR ที่ใช้ร่วมกันทุกแผนที่
        public Transform SharedCatalog;
        // SelectedMap = ลำดับแผนที่ที่เลือกอยู่
        public int SelectedMap { get; private set; }
        // IsOpen = หน้าเมนูกำลังเปิดอยู่หรือไม่ (เริ่มต้น true = เปิดตอนเริ่มเกม)
        public bool IsOpen { get; private set; } = true;
        // returnPosition = ตำแหน่งที่ผู้เล่นจะกลับไปยืน (ของแผนที่ที่เลือก)
        Vector3 returnPosition;
        // returnRotation = การหมุนที่จะกลับไปใช้
        Quaternion returnRotation;
        // entered = เคยเข้าแผนที่ที่เลือกนี้แล้วหรือยัง (ใช้เลือกว่าจะไปจุดเกิดหรือจุดเดิม)
        bool entered;
        // สไตล์ตัวอักษรของหน้า IMGUI: หัวเรื่อง, หัวรอง, เนื้อหา, ปุ่ม (สร้างครั้งแรกที่วาด)
        GUIStyle title, subtitle, body, button;
        // Start(): ตอนเริ่ม — ถ้ามีแผนที่ ให้เลือกแผนที่ลำดับ 0, แสดงเมนู,
        // และสั่ง FloorPlanImport (ถ้ามีบน GameObject เดียวกัน) กู้แปลนที่เคยนำเข้าไว้ (Restore)
        void Start() { if(Maps!=null && Maps.Length>0) SelectMap(0); Show(); GetComponent<FloorPlanImport>()?.Restore(); }
        // SelectMap(index): เลือกแผนที่ลำดับ index
        public void SelectMap(int index)
        {
            // ถ้าเมนูไม่ได้เปิดอยู่ หรือไม่มีแผนที่ หรือ index อยู่นอกช่วง ให้ออก
            if(!IsOpen || Maps==null || index<0 || index>=Maps.Length) return;
            // ยกเลิกชิ้นที่ถืออยู่ในห้อง
            Player.Room.Cancel();
            // จำลำดับแผนที่ที่เลือก
            SelectedMap=index;
            // เปิดใช้งานเฉพาะ GameObject ของแผนที่ที่เลือก ปิดที่เหลือ
            for(int i=0;i<Maps.Length;i++) Maps[i].gameObject.SetActive(i==index);
            // map = แผนที่ที่เลือก, room = RoomSandbox ของผู้เล่น
            var map=Maps[index]; var room=Player.Room;
            // ตั้งค่าห้องให้ตรงกับแผนที่: ราก FurnitureRoot, ขนาด HalfSize, และ LayoutKey (ใช้ตั้งชื่อไฟล์เซฟ)
            room.FurnitureRoot=map.Furniture; room.HalfSize=map.HalfSize; room.LayoutKey=map.Id;
            // ล้างรายการ Items แล้วดึงเฟอร์นิเจอร์ที่อยู่ใต้ Furniture ของแผนที่นี้มาใส่
            room.Items.Clear(); room.Items.AddRange(map.Furniture.GetComponentsInChildren<FurnitureItem>(true));
            // โหลดสถานะ 'เคยเข้าหรือยัง' และตำแหน่ง/การหมุนที่จำไว้ ของแผนที่นี้
            entered=map.Visited; returnPosition=map.ReturnPosition; returnRotation=map.ReturnRotation;
            // ย้ายแผงแคตตาล็อกร่วมไปติดผนังด้านหลังของห้อง (z = -ครึ่งความลึก + 0.14, สูง 1.55 ม.)
            if(SharedCatalog) SharedCatalog.position=new Vector3(0,1.55f,-map.HalfSize.y+.14f);
            // ตั้งข้อความป้ายแผนที่ = ชื่อ + ขึ้นบรรทัดใหม่ + คำอธิบาย
            if(MapLabel) MapLabel.text=map.Title+"\n"+map.Description;
            // แจ้ง 'Selected: ชื่อแผนที่'
            room.Message("Selected: "+map.Title);
        }
        // Show(): กลับมาที่หน้าเมนู (จากในห้อง)
        public void Show()
        {
            // ถ้าเมนูคลังเฟอร์นิเจอร์เปิดอยู่ ปิดมัน
            if(Player.Room.AssetMenu) Player.Room.AssetMenu.Close();
            // ยกเลิกชิ้นที่ถืออยู่
            Player.Room.Cancel();
            // ถ้าก่อนหน้านี้อยู่ในห้อง (เมนูยังไม่เปิด) จำตำแหน่ง/การหมุนปัจจุบันของผู้เล่น
            if(!IsOpen) { returnPosition=Player.transform.position; returnRotation=Player.transform.rotation; }
            // ถ้ามีแผนที่และก่อนหน้านี้อยู่ในห้อง
            if(Maps!=null && Maps.Length>0 && !IsOpen)
            // บันทึกตำแหน่ง/การหมุน/สถานะ 'เคยเข้า' ลงในข้อมูลของแผนที่นั้น
            { var map=Maps[SelectedMap]; map.ReturnPosition=returnPosition; map.ReturnRotation=returnRotation; map.Visited=entered; }
            // ตั้ง IsOpen=true, ซ่อนโลกภายใน, แสดงฉากต้อนรับ
            IsOpen=true; Interior.SetActive(false); Welcome.SetActive(true);
            // ย้ายผู้เล่นไปที่ (0,0,-8) หน้าฉากต้อนรับ ไม่หมุน
            Player.transform.SetPositionAndRotation(new Vector3(0,0,-8),Quaternion.identity);
            // ถ้าไม่ใช่ VR รีเซ็ตมุมมองกล้องบนจอคอม
            if(!XRSettings.isDeviceActive) Player.ResetDesktopView();
        }
        // Enter(): เข้าสู่ห้องของแผนที่ที่เลือก
        public void Enter()
        {
            // แสดงโลกภายใน, ซ่อนฉากต้อนรับ, ตั้ง IsOpen=false
            Interior.SetActive(true); Welcome.SetActive(false); IsOpen=false;
            // spawn = จุดเกิดของแผนที่ที่เลือก (ถ้าไม่มีแผนที่ใช้ (0,0,-4))
            Vector3 spawn=Maps!=null && Maps.Length>0?Maps[SelectedMap].Spawn:new Vector3(0,0,-4);
            // ถ้าเคยเข้าแล้ว แต่ตำแหน่งที่จำไว้อยู่นอกขอบเขตเดินได้ (เช่น แปลนถูกแทนที่/ปรับขนาด) ให้ถือว่ายังไม่เคยเข้า
            // เพื่อไปเกิดที่จุดเกิดใหม่แทน
            if(entered && Maps!=null && Maps.Length>0 && Maps[SelectedMap].WalkBoundary!=null && !PlanWalkBoundary.Contains(Maps[SelectedMap].WalkBoundary,new Vector2(returnPosition.x,returnPosition.z),.3f)) entered=false;
            // ย้ายผู้เล่น: ถ้าเคยเข้า → ไปตำแหน่งเดิมพร้อมการหมุนเดิม ไม่งั้นไปจุดเกิด หันตรง
            Player.transform.SetPositionAndRotation(entered?returnPosition:spawn,entered?returnRotation:Quaternion.identity);
            // ถ้าไม่ใช่ VR รีเซ็ตมุมมองกล้องบนจอคอม
            if(!XRSettings.isDeviceActive) Player.ResetDesktopView();
            // ทำเครื่องหมายว่าเข้าแล้ว
            entered=true;
        }
        // OnGUI(): วาดหน้าเมนูหลักด้วย IMGUI (ทำงานเฉพาะบนจอ ไม่ใช่ VR)
        void OnGUI()
        {
            // ถ้าเมนูไม่ได้เปิด หรือใช้ VR ไม่ต้องวาด
            if(!IsOpen || XRSettings.isDeviceActive) return;
            // ถ้าหน้าต่างนำเข้าแปลนเปิดอยู่ ให้หน้านั้นวาดแทน (ไม่วาดซ้อน)
            var import=GetComponent<FloorPlanImport>(); if(import && import.IsOpen) return;
            // ถ้ายังไม่เคยสร้างสไตล์ ให้สร้าง:
            if(title==null)
            {
                // หัวเรื่องใหญ่ (52 ตัวหนา สีครีม)
                title=new GUIStyle(GUI.skin.label){fontSize=52,fontStyle=FontStyle.Bold}; title.normal.textColor=new Color(.93f,.91f,.84f);
                // หัวรอง (17 สีทอง)
                subtitle=new GUIStyle(GUI.skin.label){fontSize=17}; subtitle.normal.textColor=new Color(.77f,.64f,.43f);
                // เนื้อหา (19 ตัดบรรทัด สีเทาอมเขียว)
                body=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true}; body.normal.textColor=new Color(.72f,.78f,.77f);
                // ปุ่ม (21 ตัวหนา)
                button=new GUIStyle(GUI.skin.button){fontSize=21,fontStyle=FontStyle.Bold};
            }
            // เก็บ matrix เดิม แล้วปรับสเกลให้เลย์เอาต์ 1280×720 พอดีกับขนาดจอจริง
            var old=GUI.matrix; GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
            // วาดพื้นหลังสีเขียวเข้มเต็มหน้า 1280×720 แล้วคืนสีเดิม
            var color=GUI.color; GUI.color=new Color(.045f,.09f,.085f); GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture); GUI.color=color;
            // ข้อความแถบบน: ถ้ามีแผนที่ แสดง 'THE SPACE COLLECTION / จำนวนแผนที่' ไม่งั้นแสดง 'MODERN RESIDENCE / 01'
            GUI.Label(new Rect(90,75,1050,35),Maps!=null && Maps.Length>0?"HOME / SANDBOX                                      THE SPACE COLLECTION   /   "+Maps.Length.ToString("00"):"HOME / SANDBOX                                      MODERN RESIDENCE   /   01",subtitle);
            // หมายเหตุเดิม: วาดจุดเข้าสู่หน้านำเข้าแปลนหลังพื้นหลังของเมนูนี้ ด้วยสเกลเดียวกัน
            // Draw the entry point after this menu's background, with the same scaling.
            // ถ้ามี FloorPlanImport และกดปุ่ม 'IMPORT FLOOR PLAN'
            if(import && GUI.Button(new Rect(850,65,342,48),"IMPORT FLOOR PLAN",button))
            {
                // เปิดหน้าต่างนำเข้าแปลน
                import.Open();
                // คืน GUI.matrix เดิม
                GUI.matrix=old;
                // ออกจาก OnGUI (ไม่วาดส่วนที่เหลือในเฟรมนี้)
                return;
            }
            // ถ้ามีแผนที่ให้วางเมนูแบบเลือกแผนที่ (DrawMaps)
            if(Maps!=null && Maps.Length>0)
            {
                // วาดแล้วคืน matrix และออก
                DrawMaps(); GUI.matrix=old; return;
            }
            // (กรณีไม่มีแผนที่) หัวเรื่อง 'Make room for your imagination.'
            GUI.Label(new Rect(90,157,1100,140),"Make room\nfor your imagination.",title);
            // ข้อความหัวรอง: ห้องนอน 2 / ห้องน้ำ 2 / ครัวบิลท์อิน
            GUI.Label(new Rect(94,330,1000,38),"2 BEDROOMS     /     2 BATHROOMS     /     BUILT-IN KITCHEN",subtitle);
            // ข้อความอธิบายคอนโด 120 ตร.ม.
            GUI.Label(new Rect(94,389,790,90),"Explore a modern 120 m² condo. Arrange furniture, refine your space and save your own layout.",body);
            // ปุ่มเข้าห้อง: ข้อความเป็น 'RESUME DESIGN' ถ้าเคยเข้าแล้ว ไม่งั้น 'ENTER RESIDENCE' กดแล้วเรียก Enter()
            if(GUI.Button(new Rect(94,508,360,64),entered?"RESUME DESIGN   >":"ENTER RESIDENCE   >",button)) Enter();
            // ข้อความวิธีควบคุม: คอมพิวเตอร์ (WASD, คลิกขวาหมุนมุมมอง, คลิกจัดเรียง) / Quest (trigger ขวาเลือก, trigger ซ้ายเทเลพอร์ต)
            GUI.Label(new Rect(94,610,1050,65),"DESKTOP  WASD to walk · Right mouse to look · Click to arrange\nQUEST  Right trigger to select · Left trigger to teleport",body);
            // คืน matrix เดิม
            GUI.matrix=old;
        }
        // DrawMaps(): วาดการ์ดเลือกแผนที่
        void DrawMaps()
        {
            // หัวเรื่อง 'Choose your next space.'
            GUI.Label(new Rect(90,125,1100,75),"Choose your next space.",title);
            // ข้อความแนะนำ: เลือกแปลนหรือนำเข้ารูปของตัวเอง เลย์เอาต์เซฟแยกกัน
            GUI.Label(new Rect(94,205,1100,40),"Choose a floor plan or import your own image. Layouts save separately.",body);
            // วนทุกแผนที่ สร้างการ์ดคนละใบ
            for(int i=0;i<Maps.Length;i++)
            {
                // map = แผนที่ i, slot = ความกว้างช่องต่อแผนที่ (1120 หารเท่าๆ กัน), card = ความกว้างการ์ด (หักช่องว่าง 18), x = ตำแหน่งซ้ายของการ์ด
                var map=Maps[i]; float slot=1120f/Maps.Length, card=slot-18; float x=90+i*slot;
                // เลือกสีพื้นการ์ด: ถ้าเป็นแผนที่ที่เลือกอยู่ สีสว่างกว่า ไม่งั้นสีเข้ม
                var previous=GUI.color; GUI.color=i==SelectedMap?new Color(.24f,.33f,.29f):new Color(.10f,.15f,.15f);
                // วาดพื้นการ์ดแล้วคืนสีเดิม
                GUI.DrawTexture(new Rect(x,268,card,284),Texture2D.whiteTexture); GUI.color=previous;
                // ถ้าแผนที่มีรูปตัวอย่าง วาดบนการ์ด (ย่อพอดีรักษาสัดส่วน)
                if(map.Preview) GUI.DrawTexture(new Rect(x+10,278,card-20,152),map.Preview,ScaleMode.ScaleToFit);
                // ชื่อแผนที่ (สไตล์หัวรอง)
                GUI.Label(new Rect(x+14,438,card-28,30),map.Title,subtitle);
                // คำอธิบายแผนที่ (สไตล์เนื้อหา)
                GUI.Label(new Rect(x+14,472,card-28,80),map.Description,body);
                // ปุ่มโปร่งใสครอบการ์ดทั้งใบ กดแล้วเลือกแผนที่นั้น (SelectMap(i))
                if(GUI.Button(new Rect(x,268,card,284),GUIContent.none,GUIStyle.none)) SelectMap(i);
            }
            // selected = แผนที่ที่เลือกอยู่
            var selected=Maps[SelectedMap];
            // ข้อความ 'SELECTED / ชื่อ' (และ 'Layout kept in this session' ถ้าเคยเข้าแล้ว)
            GUI.Label(new Rect(94,582,655,32),"SELECTED / "+selected.Title+(selected.Visited?"  ·  Layout kept in this session":""),subtitle);
            // ปุ่ม 'ENTER THIS MAP' (หรือ 'RESUME THIS MAP' ถ้าเคยเข้า) กดแล้ว Enter()
            if(GUI.Button(new Rect(850,580,342,58),selected.Visited?"RESUME THIS MAP  >":"ENTER THIS MAP  >",button)) Enter();
            // ข้อความล่าง: กลับด้วย MAIN MENU; SAVE/LOAD แยกอิสระต่อแผนที่
            GUI.Label(new Rect(94,650,1100,35),"Return with MAIN MENU · SAVE / LOAD are independent for each map",body);
        }
    }
}
