// ==== AssetMenu.cs ====
// เมนูคลังเฟอร์นิเจอร์ (Apartment Kit) มี 2 รูปแบบ: แผงปุ่ม 3 มิติลอยหน้าผู้เล่นใน VR และหน้าต่าง IMGUI บนจอคอมพิวเตอร์
// เลือกชิ้นจากคลัง (ApartmentLibrary) แล้วสั่ง RoomSandbox.BeginAsset เพื่อถือไปวางในห้อง
// นำเข้า UnityEngine
using UnityEngine;
// นำเข้า UnityEngine.XR — ใช้ XRSettings.isDeviceActive เช็คว่ากำลังใช้แว่น VR หรือไม่
using UnityEngine.XR;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส AssetMenu (MonoBehaviour)
    public sealed class AssetMenu : MonoBehaviour
    {
        // Room = ตัวจัดการห้อง (RoomSandbox) ที่เมนูนี้สั่งงาน
        public RoomSandbox Room;
        // Player = ตัวควบคุมผู้เล่น (SandboxInput) ใช้รู้ตำแหน่ง/ทิศกล้อง
        public SandboxInput Player;
        // IsOpen = เมนูเปิดอยู่หรือไม่ (อ่านได้จากภายนอก แต่ตั้งค่าได้เฉพาะในคลาสนี้)
        public bool IsOpen { get; private set; }
        // panel = GameObject ของแผงปุ่ม 3 มิติ (ใช้ใน VR)
        GameObject panel;
        // material = วัสดุสีของปุ่ม 3 มิติ (สร้างครั้งแรกที่ใช้)
        Material material;
        // page = หน้าปัจจุบันของรายการ (เริ่มที่ 0)
        int page;
        // search = ข้อความค้นหาในช่องค้นหา
        string search="";
        // PageSize = จำนวนรายการต่อหน้า (8)
        const int PageSize=8;
        // previews = ตัวสร้าง/แคชรูปตัวอย่างเฟอร์นิเจอร์
        readonly FurniturePreview previews=new FurniturePreview();
        // previewPage = หน้าที่รูปตัวอย่างชุดปัจจุบันถูกสร้างไว้ (-1 = ยังไม่มี)
        int previewPage=-1;
        // previewSearch = คำค้นที่รูปตัวอย่างชุดปัจจุบันถูกสร้างไว้
        string previewSearch;
        // cardLabel = สไตล์ข้อความบนการ์ด (สร้างครั้งแรกที่วาด)
        GUIStyle cardLabel;
        // Toggle(): สลับเปิด/ปิดเมนู
        // ถ้าหน้าเริ่มต้น (StartScreen) เปิดอยู่ ไม่ทำอะไร; ถ้าเมนูเปิดอยู่ → ปิด;
        // ไม่เช่นนั้น → ปิดเมนูห้องของ Player ก่อน ตั้ง IsOpen=true แล้วสร้างแผง (BuildPanel)
        public void Toggle() { if(Room.StartScreen.IsOpen) return; if(IsOpen) Close(); else { Player.CloseRoomMenu(); IsOpen=true; BuildPanel(); } }
        // Close(): ปิดเมนู — ตั้ง IsOpen=false, ล้างรูปตัวอย่าง, ปิดเมนูห้องของ Player (ถ้ามี)
        // และซ่อนพร้อมทำลายแผง 3 มิติ (ถ้ามี)
        public void Close() { IsOpen=false;previews.Clear(); if(Player) Player.CloseRoomMenu(); if(panel) { panel.SetActive(false); Destroy(panel); } }
        // ChangePage(direction): เปลี่ยนหน้าไปข้างหน้า (+1) หรือถอยหลัง (-1)
        public void ChangePage(int direction)
        {
            // max = เลขหน้าสุดท้าย = (จำนวนรายการ-1)/PageSize; จากนั้น page = page+direction จำกัดให้อยู่ในช่วง 0..max
            int max=Mathf.Max(0,(Room.Library.Entries.Length-1)/PageSize); page=Mathf.Clamp(page+direction,0,max);
            // สร้างแผง 3 มิติใหม่ให้แสดงหน้าใหม่
            BuildPanel();
        }
        // Spawn(index): เลือกรายการลำดับ index จากคลัง
        public void Spawn(int index)
        // ถ้า index อยู่นอกช่วง ออกเลย; ไม่เช่นนั้นสั่งห้องให้เริ่มถือ asset ชิ้นนั้น (BeginAsset ด้วย Key)
        // แล้วปิดเมนู
        { if(index<0 || index>=Room.Library.Entries.Length) return; Room.BeginAsset(Room.Library.Entries[index].Key); Close(); }
        // BuildPanel(): สร้างแผงปุ่ม 3 มิติสำหรับ VR
        void BuildPanel()
        {
            // ถ้ามีแผงเก่าอยู่ ให้ซ่อนและทำลายก่อน
            if(panel) { panel.SetActive(false); Destroy(panel); }
            // ถ้ายังไม่มีวัสดุ สร้างวัสดุ Standard สีเขียวเข้ม
            if(!material) { material=new Material(Shader.Find("Standard")); material.color=new Color(.07f,.17f,.16f); }
            // สร้าง GameObject ใหม่เป็นแผง
            panel=new GameObject("Apartment catalog / runtime");
            // หาทิศหน้าของกล้องผู้เล่น ตัดแกน y ทิ้ง (ให้แผงตั้งตรง)
            // ถ้าทิศสั้นเกือบเป็นศูนย์ (มองขึ้น/ลงตรงๆ) ใช้ Vector3.forward แทน แล้ว Normalize
            var forward=Player.View.transform.forward; forward.y=0; if(forward.sqrMagnitude<.01f) forward=Vector3.forward; forward.Normalize();
            // วางแผงไว้ข้างหน้าผู้เล่นห่างออกไป 1.8 เมตร
            panel.transform.position=Player.View.transform.position+forward*1.8f;
            // หมุนแผงให้หันหน้าไปตามทิศ forward (แกน Z ของแผงชี้ไปทางเดียวกับที่ผู้เล่นมอง)
            panel.transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
            // สร้างข้อความหัวเรื่อง 'APARTMENT KIT / เลขหน้า' ที่มุมซ้ายบนของแผง
            Label("APARTMENT KIT / "+(page+1),new Vector3(-.95f,.77f,0),.018f);
            // วนแถวทั้ง 8 แถวของหน้านี้
            for(int row=0;row<PageSize;row++)
            {
                // คำนวณลำดับรายการ = หน้า×8 + แถว; ถ้าเกินจำนวนรายการให้หยุดวน
                int index=page*PageSize+row; if(index>=Room.Library.Entries.Length) break;
                // ดึงรายการ แล้วสร้างปุ่มชื่อรายการ (command 14 = Spawn, id = ลำดับรายการ) เรียงลงมาแถวละ 0.145 ม.
                var entry=Room.Library.Entries[index]; Button(entry.Name,new Vector3(0,.5f-row*.145f,0),14,index);
            }
            // ปุ่ม '< PREVIOUS' (command 13 = เปลี่ยนหน้า, id = -1) ความกว้าง 0.58
            Button("< PREVIOUS",new Vector3(-.6f,-.82f,0),13,-1,.58f);
            // ปุ่ม 'NEXT >' (command 13, id = +1)
            Button("NEXT >",new Vector3(0,-.82f,0),13,1,.58f);
            // ปุ่ม 'CLOSE' (command 12 = สลับเมนู)
            Button("CLOSE",new Vector3(.6f,-.82f,0),12,0,.58f);
            // แสดงแผงเฉพาะตอนใช้ VR จริง (บนจอคอมใช้ IMGUI แทน)
            panel.SetActive(XRSettings.isDeviceActive);
        }
        // Label(text,position,size): สร้างข้อความ 3 มิติ (TextMesh)
        void Label(string text,Vector3 position,float size)
        {
            // สร้าง GameObject ชื่อเดียวกับข้อความ เป็นลูกของแผง และตั้งตำแหน่งโลคอล
            var go=new GameObject(text); go.transform.SetParent(panel.transform,false); go.transform.localPosition=position;
            // เพิ่ม TextMesh: ตั้งข้อความ, ขนาดฟอนต์ 48, ขนาดตัวอักษรตาม size, จัดชิดมุมซ้ายบน
            var mesh=go.AddComponent<TextMesh>(); mesh.text=text; mesh.fontSize=48; mesh.characterSize=size; mesh.anchor=TextAnchor.UpperLeft;
        }
        // Button(label,pos,command,id,width): สร้างปุ่ม 3 มิติ (ค่าเริ่มต้นกว้าง 1.9)
        void Button(string label,Vector3 pos,int command,int id,float width=1.9f)
        {
            // สร้างลูกบาศก์ เป็นลูกของแผง ตั้งตำแหน่ง
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.SetParent(panel.transform,false); go.transform.localPosition=pos;
            // ปรับสเกลให้เป็นแผ่นแบน (กว้าง×0.13×0.025) และใส่วัสดุ
            go.transform.localScale=new Vector3(width,.13f,.025f); go.GetComponent<Renderer>().sharedMaterial=material;
            // ติด RoomButton แล้วตั้ง Command กับ ItemId (ให้เลเซอร์ใน SandboxInput ตรวจเจอและเรียก Room.Command)
            var b=go.AddComponent<RoomButton>(); b.Command=command; b.ItemId=id;
            // สร้างข้อความป้ายบนปุ่ม วางชิดซ้ายและยื่นออกมาด้านหน้าเล็กน้อย
            Label(label,pos+new Vector3(-width/2+.02f,.052f,-.025f),.009f);
        }
        // OnGUI(): วาดเมนูแบบ IMGUI สำหรับหน้าจอคอมพิวเตอร์ (ถูกเรียกหลายครั้งต่อเฟรม)
        void OnGUI()
        {
            // ถ้าหน้าเริ่มต้นเปิดอยู่ หรือกำลังใช้ VR → ไม่วาด
            if(Room.StartScreen.IsOpen || XRSettings.isDeviceActive) return;
            // วาดปุ่มมุมขวาบน 'ASSETS [Tab]' / 'CLOSE ASSETS [Tab]' กดแล้ว Toggle()
            if(GUI.Button(new Rect(Screen.width-252,12,130,32),IsOpen?"CLOSE ASSETS [Tab]":"ASSETS [Tab]")) Toggle();
            // ถ้าเมนูไม่ได้เปิด จบการวาด
            if(!IsOpen) return;
            // ถ้าเปลี่ยนหน้าหรือเปลี่ยนคำค้น ให้ล้างรูปตัวอย่างเก่าและจำค่าใหม่
            if(previewPage!=page || previewSearch!=search) { previews.Clear();previewPage=page;previewSearch=search; }
            // สร้างสไตล์ข้อความการ์ด (กึ่งกลาง, ตัดบรรทัด, ฟอนต์ 12) ถ้ายังไม่มี
            if(cardLabel==null) cardLabel=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=12};
            // เก็บ GUI.matrix เดิมไว้ แล้วคำนวณสเกล = min(กว้างจอ/900, สูงจอ/700)
            // ตั้ง GUI.matrix เป็นสเกลนั้น เพื่อให้เลย์เอาต์คงที่ขนาด 900×700 ปรับตามขนาดหน้าจอ
            var old=GUI.matrix; float scale=Mathf.Min(Screen.width/900f,Screen.height/700f); GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            // วาดพื้นหลังสี่เหลี่ยมสีเข้ม: ตั้ง GUI.color แล้ววาดพื้นขาวทับ (จึงออกมาเป็นสีที่ตั้ง) แล้วคืนสีเดิม
            var backdrop=GUI.color; GUI.color=new Color(.09f,.13f,.14f); GUI.DrawTexture(new Rect(25,65,650,585),Texture2D.whiteTexture); GUI.color=backdrop;
            // วาดกรอบหน้าต่างพร้อมหัวข้อ 'APARTMENT KIT / FURNITURE LIBRARY'
            GUI.Box(new Rect(25,65,650,585),"APARTMENT KIT / FURNITURE LIBRARY");
            // ข้อความแนะนำ: ราคาตัวอย่างเป็นบาท คลิกโมเดลแล้วคลิกพื้นเพื่อวาง
            GUI.Label(new Rect(45,95,570,24),"Sample prices in THB. Click a model, then click the floor to place it.");
            // ช่องค้นหา: ถ้าข้อความเปลี่ยน เก็บค่าใหม่และกลับไปหน้า 0
            string next=GUI.TextField(new Rect(45,128,485,28),search); if(next!=search) { search=next; page=0; }
            // ปุ่ม 'Close' ปิดเมนู
            if(GUI.Button(new Rect(540,128,110,28),"Close")) Close();
            // found = จำนวนรายการที่ผ่านตัวกรอง (นับสะสม), shown = จำนวนการ์ดที่วาดแล้วในหน้านี้
            int found=0,shown=0;
            // วนทุกรายการในคลัง
            for(int i=0;i<Room.Library.Entries.Length;i++)
            {
                // ดึงรายการ e; ถ้ามีคำค้น และ (ชื่อ+หมวด) ไม่มีคำค้นอยู่ (ไม่สนตัวพิมพ์เล็กใหญ่) ให้ข้าม
                var e=Room.Library.Entries[i]; if(!string.IsNullOrEmpty(search) && (e.Name+e.Category).IndexOf(search,System.StringComparison.OrdinalIgnoreCase)<0) continue;
                // ข้ามรายการของหน้าก่อนหน้า (found++ < page×8) หรือถ้าวางครบ 8 การ์ดแล้ว
                // (found++ เพิ่มตัวนับทุกครั้งที่ผ่านตัวกรอง)
                if(found++<page*PageSize || shown>=PageSize) continue;
                // คำนวณตำแหน่งการ์ด: 4 คอลัมน์ต่อแถว ห่างกันคอลัมน์ละ 152 พิกเซล แถวละ 187
                float x=45+(shown%4)*152,y=170+(shown/4)*187;
                // สร้างสี่เหลี่ยมการ์ดขนาด 144×179
                var card=new Rect(x,y,144,179);
                // วาดกรอบการ์ด (ไม่มีข้อความ)
                GUI.Box(card,GUIContent.none);
                // วาดรูปเฉพาะรอบ Repaint (IMGUI จะเรียก OnGUI หลายรอบ แต่เรนเดอร์ได้เฉพาะ Repaint)
                if(Event.current.type==EventType.Repaint)
                {
                    // ขอรูปตัวอย่างจากตัวสร้างรูป (เรนเดอร์ครั้งแรก แล้วแคชไว้)
                    var texture=previews.Get(e);
                    // ถ้ามีรูป วาดลงในการ์ดแบบย่อให้พอดี (รักษาสัดส่วน)
                    if(texture) GUI.DrawTexture(new Rect(x+5,y+5,134,104),texture,ScaleMode.ScaleToFit);
                }
                // ข้อความชื่อรายการใต้รูป
                GUI.Label(new Rect(x+5,y+109,134,43),e.Name,cardLabel);
                // ข้อความล่างสุด: ถ้ามีราคาแสดง 'THB ราคา' (จัดรูปแบบมีจุลภาค) ไม่งั้นแสดงหมวดหมู่
                GUI.Label(new Rect(x+5,y+151,134,23),e.HasPrice?"THB "+e.PriceTHB.ToString("N0"):e.Category,cardLabel);
                // ปุ่มโปร่งใสครอบทั้งการ์ด (พร้อม tooltip) — กดแล้วเรียก Spawn(i) เพื่อหยิบชิ้นนั้นไปวาง
                if(GUI.Button(card,new GUIContent("",e.Category+" / "+e.Name+" - Click to place"),GUIStyle.none)) Spawn(i);
                // นับจำนวนการ์ดที่วาดแล้ว +1
                shown++;
            }
            // ปุ่ม '< Previous' ลดเลขหน้าไม่ต่ำกว่า 0
            if(GUI.Button(new Rect(45,560,150,34),"< Previous")) page=Mathf.Max(0,page-1);
            // ข้อความ 'Page x / y' (y = จำนวนหน้าทั้งหมดที่ปัดขึ้น อย่างน้อย 1)
            GUI.Label(new Rect(220,565,200,30),"Page "+(page+1)+" / "+Mathf.Max(1,Mathf.CeilToInt(found/(float)PageSize)));
            // ปุ่ม 'Next >' เพิ่มเลขหน้าไม่เกินหน้าสุดท้าย
            if(GUI.Button(new Rect(495,560,150,34),"Next >")) page=Mathf.Min(Mathf.Max(0,(found-1)/PageSize),page+1);
            // ข้อความแนะนำการใช้งาน: คลิกชิ้นที่วางแล้วเพื่อยกขึ้น, R หมุน, Delete ลบ, Esc ยกเลิก
            GUI.Label(new Rect(45,610,580,25),"Click an existing piece to pick it up · R rotate · Delete remove · Esc cancel");
            // คืนค่า GUI.matrix เดิม
            GUI.matrix=old;
        }
        // OnDestroy(): ทำลายตัวสร้างรูปตัวอย่าง, แผง, และวัสดุ เพื่อไม่ให้หน่วยความจำรั่ว
        void OnDestroy() { previews.Dispose();if(panel) Destroy(panel); if(material) Destroy(material); }
    }
}
