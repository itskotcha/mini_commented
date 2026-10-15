// ==== FloorPlanImport.cs ====
// ระบบ 'นำเข้าแปลนพื้นจากรูปภาพ' ครบวงจร: โหลดรูป PNG/JPG → ตรวจจับกำแพง (FloorPlanDetector) → ให้ผู้ใช้แก้หน้าต่าง/ประตูในหน้าต่าง IMGUI
// → สร้างห้อง 3 มิติ (พื้น, กำแพง, หน้าต่าง, ประตู) เป็นแผนที่ใหม่ → บันทึกแปลนลงเครื่อง และกู้คืนตอนเปิดเกมครั้งถัดไป
// นำเข้า System — ใช้ Action, ArgumentException, Environment
using System;
// นำเข้า System.Collections.Generic — ใช้ List<T>
using System.Collections.Generic;
// นำเข้า System.IO — ใช้ File, Directory, Path, FileInfo
using System.IO;
// นำเข้า UnityEngine
using UnityEngine;
// นำเข้า UnityEngine.XR — ใช้ XRSettings
using UnityEngine.XR;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส FloorPlanImport (MonoBehaviour)
    public sealed class FloorPlanImport : MonoBehaviour
    {
        // คลาสย่อย Plan = ข้อมูลแปลนที่ตรวจจับได้ (เก็บเป็น JSON ได้): width/depth = ขนาดเป็นเมตร, key = รหัสแปลน (hash),
        // sourceCrop = ส่วนของรูปที่ครอป, walls = รายการกำแพง (สี่เหลี่ยม), windows = รายการหน้าต่าง, doors = รายการประตู
        [Serializable] public class Plan { public float width,depth; public string key; public Rect sourceCrop; public List<Rect> walls=new List<Rect>(); public List<Rect> windows=new List<Rect>(); public List<PlanDoorData> doors=new List<PlanDoorData>(); }
        // Flow = หน้าเมนูหลัก (StartScreen) ที่ใช้ตรวจว่าเมนูเปิดอยู่และใช้เข้าถึงผู้เล่น/แผนที่
        public StartScreen Flow;
        // image = รูปแปลนที่โหลดอยู่
        Texture2D image;
        // draft = แปลนฉบับร่างที่ตรวจจับได้ ยังไม่ได้สร้างเป็นแผนที่ (null = ยังไม่ได้ตรวจจับ)
        Plan draft;
        // imageCrop = ส่วนของรูปที่แสดง/ใช้ (ค่า 0–1) เริ่มต้นทั้งภาพ
        Rect imageCrop=new Rect(0,0,1,1);
        // open = หน้าต่างนำเข้าเปิดอยู่หรือไม่; browse = กำลังแสดงหน้าต่างเลือกไฟล์หรือไม่
        bool open,browse;
        // editWindows = โหมดแก้หน้าต่างเปิดอยู่หรือไม่
        bool editWindows;
        // editDoors = โหมดแก้ประตูเปิดอยู่หรือไม่
        bool editDoors;
        // selectedDoor = ลำดับประตูที่เลือกอยู่ (-1 = ไม่ได้เลือก)
        int selectedDoor=-1;
        // doorStart = จุดคลิกแรกของการเพิ่มประตู (Vector2? = ค่าว่างได้ หมายถึงยังไม่ได้คลิก)
        Vector2? doorStart;
        // windowStart = จุดคลิกแรกของการเพิ่มหน้าต่าง (ค่าว่างได้)
        Vector2? windowStart;
        // IsOpen: อ่านสถานะ open จากภายนอก (StartScreen ใช้เพื่อหยุดวางหน้าของตัวเอง)
        public bool IsOpen => open;
        // Open(): เปิดหน้าต่างนำเข้า และปิดหน้าต่างเลือกไฟล์
        public void Open() { open=true; browse=false; }
        // filePath = พาธรูปที่พิมพ์, folder = โฟลเดอร์ที่กำลังเรียกดู, status = ข้อความสถานะ (เริ่มต้นเป็นคำแนะนำรูปแบบรูป)
        string filePath="",folder="",status="PNG/JPG, top-down, dark walls on a white background.";
        // width = ความกว้างแปลน (เมตร) เริ่ม 10, threshold = เกณฑ์ความมืดของพิกเซล .35, minWall = ความยาวกำแพงขั้นต่ำ (เมตร) .35
        float width=10,threshold=.35f,minWall=.35f;
        // widthText = ข้อความในช่องกรอกความกว้าง
        string widthText="10.00";
        // scroll = ตำแหน่งเลื่อนของรายการไฟล์
        Vector2 scroll;
        // files = รายการรูปในโฟลเดอร์, directories = รายการโฟลเดอร์ย่อย (เริ่มเป็นอาร์เรย์ว่าง)
        string[] files=Array.Empty<string>(),directories=Array.Empty<string>();
        // วัสดุที่สร้างเอง: กำแพง, พื้น, กรอบ, กระจก
        Material wallMaterial,floorMaterial,frameMaterial,glassMaterial;
        // ownedImages = รูปที่คลาสนี้สร้างขึ้น (ไว้ทำลายตอนจบ ไม่ให้หน่วยความจำรั่ว)
        readonly List<Texture2D> ownedImages=new List<Texture2D>();
        // StorePath = พาธไฟล์ที่เก็บแปลนที่นำเข้า (imported-plan-map.json ในโฟลเดอร์ข้อมูลของแอป)
        string StorePath => Path.Combine(Application.persistentDataPath,"imported-plan-map.json");
        // Start(): ตั้งโฟลเดอร์เริ่มต้นของตัวเลือกไฟล์
        void Start()
        {
            // บน Android ใช้โฟลเดอร์ข้อมูลของแอป ส่วนระบบอื่นใช้โฟลเดอร์ 'รูปภาพ' (MyPictures) ของผู้ใช้
            folder=Application.platform==RuntimePlatform.Android?Application.persistentDataPath:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            // ถ้าได้ค่าว่าง ใช้โฟลเดอร์ข้อมูลของแอปแทน
            if(string.IsNullOrEmpty(folder)) folder=Application.persistentDataPath;
        }
        // Restore(): กู้แปลนที่เคยนำเข้าและบันทึกไว้ (เรียกจาก StartScreen.Start)
        public void Restore()
        {
            // try เพื่อดักข้อผิดพลาดทั้งหมด
            try
            {
                // ถ้าไม่มีไฟล์ที่บันทึกไว้ ออก
                if(!File.Exists(StorePath)) return;
                // อ่าน JSON แล้วแปลงเป็น Plan
                var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(StorePath));
                // ตรวจ ValidPlan ไม่ผ่านโยนข้อผิดพลาด
                if(!ValidPlan(plan)) throw new InvalidDataException("Invalid saved plan");
                // พาธรูปตัวอย่างที่บันทึกไว้ (imported-plan.png)
                string preview=Path.Combine(Application.persistentDataPath,"imported-plan.png");
                // ถ้ามีไฟล์รูป โหลดเป็น Texture2D และเก็บใน ownedImages
                if(File.Exists(preview)) { image=new Texture2D(2,2); image.LoadImage(File.ReadAllBytes(preview)); ownedImages.Add(image); }
                // ถ้า windows เป็น null (ไฟล์เก่า) สร้างลิสต์ว่าง
                if(plan.windows==null) plan.windows=new List<Rect>();
                // ถ้า doors เป็น null สร้างลิสต์ว่าง
                if(plan.doors==null) plan.doors=new List<PlanDoorData>();
                // ตั้ง draft=plan, width และข้อความช่องกรอก (รูปแบบทศนิยม 2 ตำแหน่ง ไม่ขึ้นกับภาษา/ภูมิภาค)
                draft=plan; width=plan.width;widthText=width.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);
                // ตั้ง imageCrop จาก sourceCrop ถ้าใช้ได้ ไม่งั้นทั้งภาพ
                imageCrop=plan.sourceCrop.width>0?plan.sourceCrop:new Rect(0,0,1,1);
                // สร้างแผนที่จากแปลน (false = ไม่ต้องเลือกแผนที่นั้นทันที)
                Install(plan,false);
            }
            // ถ้าล้มเหลว แจ้งข้อความ 'Saved plan could not be restored: ...' (ไม่ทำให้เกมล่ม)
            catch(Exception e) { status="Saved plan could not be restored: "+e.Message; }
        }
        // ValidPlan(p): ตรวจความถูกต้องของแปลนก่อนใช้ (กันไฟล์เสีย/ตัวเลขผิดปกติ/ข้อมูลที่ตัดต่อ)
        public static bool ValidPlan(Plan p)
        {
            // ไม่ผ่านถ้า: เป็น null, ตัวเลขเป็น NaN, ความกว้างหรือความลึกอยู่นอก 3–30 ม., key ว่าง หรือไม่ตรงรูปแบบ 'plan-' ตามด้วยเลขฐาน 16 แปดหลัก,
            // หรือรายการกำแพงเป็น null/ว่าง/เกิน 300
            if(p==null || float.IsNaN(p.width+p.depth) || p.width<3 || p.width>30 || p.depth<3 || p.depth>30 || string.IsNullOrEmpty(p.key) || !System.Text.RegularExpressions.Regex.IsMatch(p.key,"^plan-[0-9a-f]{8}$") || p.walls==null || p.walls.Count==0 || p.walls.Count>300) return false;
            // ตรวจกำแพงทุกชิ้น: ตัวเลขต้องเป็นจำนวนจริง, กว้าง/สูงเป็นบวก, และอยู่ในขอบเขตแปลน (เผื่อ 1 ซม.)
            foreach(var r in p.walls) if(float.IsNaN(r.x+r.y+r.width+r.height) || float.IsInfinity(r.x+r.y+r.width+r.height) || r.width<=0 || r.height<=0 || r.xMin< -p.width/2-.01f || r.xMax>p.width/2+.01f || r.yMin< -p.depth/2-.01f || r.yMax>p.depth/2+.01f) return false;
            // ถ้ามีรายการหน้าต่าง:
            if(p.windows!=null)
            {
                // ไม่ผ่านถ้ามีเกิน 100 ชิ้น
                if(p.windows.Count>100) return false;
                // ตรวจหน้าต่างทุกชิ้น: ตัวเลขจริง กว้าง/สูงเป็นบวก และอยู่ในขอบเขตแปลน
                foreach(var r in p.windows) if(float.IsNaN(r.x+r.y+r.width+r.height) || float.IsInfinity(r.x+r.y+r.width+r.height) || r.width<=0 || r.height<=0 || r.xMin< -p.width/2 || r.xMax>p.width/2 || r.yMin< -p.depth/2 || r.yMax>p.depth/2) return false;
            }
            // ถ้ามีรายการประตู:
            if(p.doors!=null)
            {
                // ไม่ผ่านถ้ามีเกิน 60 บาน
                if(p.doors.Count>60) return false;
                // ตรวจประตูทุกบาน:
                foreach(var d in p.doors)
                {
                    // ไม่ผ่านถ้าเป็น null; r = ช่องเปิดของประตู
                    if(d==null) return false;var r=d.opening;
                    // ความกว้างช่องกรอบ (passageWidth) ต้องเป็นจำนวนจริงและอยู่ในช่วง 0–1.6 ม.
                    if(float.IsNaN(d.passageWidth) || float.IsInfinity(d.passageWidth) || d.passageWidth<0 || d.passageWidth>1.6f) return false;
                    // ช่องเปิดต้องเป็นจำนวนจริง กว้าง/สูงเป็นบวก ยาว 0.4–4 ม. และอยู่ในขอบเขตแปลน
                    if(float.IsNaN(r.x+r.y+r.width+r.height) || float.IsInfinity(r.x+r.y+r.width+r.height) || r.width<=0 || r.height<=0 || d.Length<.4f || d.Length>4 || r.xMin< -p.width/2 || r.xMax>p.width/2 || r.yMin< -p.depth/2 || r.yMax>p.depth/2) return false;
                }
            }
            // ผ่านทุกข้อ คืน true
            return true;
        }
        // Detect(...): ฟังก์ชัน static ตรวจจับกำแพงจากรูป (รับรูป, ความกว้างจริงเมตร, เกณฑ์ความมืด, ความยาวขั้นต่ำ)
        public static Plan Detect(Texture2D texture,float metres,float cutoff,float minimum)
        // ส่งต่อไปที่ FloorPlanDetector.Detect แล้วคืนผลลัพธ์ (Plan)
        { return FloorPlanDetector.Detect(texture,metres,cutoff,minimum); }
        // LoadImageFile(path): โหลดรูปจากพาธ (ตรวจความปลอดภัยก่อน)
        public void LoadImageFile(string path)
        {
            // ถ้าไฟล์ไม่มี หรือใหญ่เกิน 10 MB โยนข้อผิดพลาด
            if(!File.Exists(path) || new FileInfo(path).Length>10*1024*1024) throw new ArgumentException("Image missing or larger than 10 MB.");
            // ตรวจนามสกุล ต้องเป็น .png/.jpg/.jpeg ไม่งั้นโยนข้อผิดพลาด
            string ext=Path.GetExtension(path).ToLowerInvariant(); if(ext!=".png" && ext!=".jpg" && ext!=".jpeg") throw new ArgumentException("Choose a PNG or JPG image.");
            // สร้าง Texture2D แล้วโหลดจากไบต์ของไฟล์; ถ้าโหลดไม่ได้ หรือกว้าง/สูงเกิน 4096 พิกเซล ให้ทำลายทิ้งแล้วโยนข้อผิดพลาด
            var next=new Texture2D(2,2); if(!next.LoadImage(File.ReadAllBytes(path)) || next.width>4096 || next.height>4096) { Destroy(next); throw new ArgumentException("Invalid image or image larger than 4096 px."); }
            // หมายเหตุเดิม: พรีวิวของแผนที่เก่ายังคงถือ texture เดิมไว้ขณะที่ตรวจดูรูปใหม่
            // Old map previews keep their texture while another image is being reviewed.
            // ใช้รูปใหม่: ตั้ง image, หาขอบครอปอัตโนมัติ, เก็บใน ownedImages, ล้าง draft เดิม, จำพาธ, และแจ้งขั้นตอนต่อไป
            image=next; imageCrop=FloorPlanDetector.FindCrop(image); ownedImages.Add(next); draft=null; filePath=path; status="Image loaded. Set actual cropped PLAN width in metres, then Detect walls.";
        }
        // ReviewImage(path): โหลดรูปแล้วเปิดหน้าต่างนำเข้า
        public void ReviewImage(string path) { LoadImageFile(path); open=true; }
        // PreviewCurrent(): ตรวจจับกำแพงจากรูปปัจจุบันและแสดงฉบับร่าง
        public Plan PreviewCurrent()
        // ตรวจจับด้วย width/threshold/minWall ได้ draft, อัปเดตขอบครอป, ล้างจุดคลิกค้างและประตูที่เลือก, แจ้งจำนวนกำแพง/หน้าต่าง/ประตูที่แนะนำ แล้วคืน draft
        { draft=Detect(image,width,threshold,minWall);imageCrop=draft.sourceCrop;windowStart=null;doorStart=null;selectedDoor=-1;status=draft.walls.Count+" walls / "+draft.windows.Count+" windows / "+draft.doors.Count+" door suggestions. Check orange swing arcs before generating.";return draft; }
        // SetPlanWidth(next): ปรับความกว้างจริงของแปลน (เมตร) และขยาย/ย่อกำแพง หน้าต่าง ประตู ตามสัดส่วน
        public void SetPlanWidth(float next)
        {
            // aspect = สัดส่วนความลึก/ความกว้าง (จากรูปที่ครอป ถ้าไม่มีรูปใช้จาก draft ไม่มีทั้งคู่ใช้ 1)
            float aspect=image?image.height*imageCrop.height/(image.width*imageCrop.width):(draft!=null?draft.depth/draft.width:1);
            // ถ้าค่าไม่ใช่จำนวนจริง หรือความกว้าง/ความลึกใหม่ (next×aspect) อยู่นอก 3–30 ม.
            if(float.IsNaN(next) || float.IsInfinity(next) || next<3 || next>30 || next*aspect<3 || next*aspect>30)
                // โยนข้อผิดพลาดบอกให้กรอกความกว้างของแปลนที่ครอปแล้ว
                throw new ArgumentException("Width and depth must both be 3-30 m. Enter the cropped plan width.");
            // ถ้ามีฉบับร่าง ให้ปรับขนาดข้อมูลในนั้นด้วย:
            if(draft!=null)
            {
                // factor = ตัวคูณการขยาย = ความกว้างใหม่ ÷ ความกว้างเดิม
                float factor=next/draft.width;
                // ถ้าประตูใดจะยาว <0.4 ม. หรือ >4 ม. หลังปรับ ให้โยนข้อผิดพลาด (ให้ผู้ใช้ลบ/แก้ประตูนั้นก่อน)
                if(draft.doors!=null) foreach(var door in draft.doors) if(door.Length*factor<.4f || door.Length*factor>4) throw new ArgumentException("Resizing would make a door smaller than 0.4 m or larger than 4 m. Remove or adjust that door first.");
                // ขยายกำแพงทุกชิ้นด้วย ScaleRect
                for(int i=0;i<draft.walls.Count;i++) draft.walls[i]=ScaleRect(draft.walls[i],factor);
                // ขยายหน้าต่างทุกชิ้น
                if(draft.windows!=null) for(int i=0;i<draft.windows.Count;i++) draft.windows[i]=ScaleRect(draft.windows[i],factor);
                // ขยายช่องเปิดของประตูทุกบาน
                if(draft.doors!=null) foreach(var door in draft.doors) door.opening=ScaleRect(door.opening,factor);
                // ตั้ง width/depth ใหม่ของ draft และคำนวณ key ใหม่
                draft.width=next;draft.depth*=factor;FloorPlanDetector.UpdateKey(draft);
            }
            // จำ width ใหม่ + อัปเดตข้อความช่องกรอก + ล้างจุดคลิกหน้าต่างค้าง
            width=next;widthText=width.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);windowStart=null;
            // แจ้งสถานะขนาดใหม่ (เฟอร์นิเจอร์ขนาดเท่าเดิม ต้องกด Generate เพื่อนำไปใช้กับแผนที่)
            status="Plan resized to "+width.ToString("0.00")+" x "+(width*aspect).ToString("0.00")+" m. Furniture keeps its size. Generate to apply to the map.";
        }
        // ScaleRect(r,factor): คูณตำแหน่งและขนาดของสี่เหลี่ยมด้วย factor
        static Rect ScaleRect(Rect r,float factor) => new Rect(r.x*factor,r.y*factor,r.width*factor,r.height*factor);
        // Generate(plan,persist): สร้างแผนที่จากแปลน (persist = บันทึกลงเครื่องด้วยหรือไม่)
        public void Generate(Plan plan,bool persist=true)
        {
            // ต้องเปิดจากหน้าเมนูหลักเท่านั้น และแปลนต้องผ่าน ValidPlan ไม่งั้นโยนข้อผิดพลาด
            if(!Flow.IsOpen || !ValidPlan(plan)) throw new ArgumentException("Review a valid plan from the main menu first.");
            // สร้างห้อง 3 มิติและเพิ่มเป็นแผนที่ (true = เลือกแผนที่นี้ด้วย)
            Install(plan,true);
            // ถ้า persist: เขียน JSON ของแปลนลง StorePath และเขียนรูปเป็น PNG (imported-plan.png) เพื่อกู้คืนภายหลัง
            if(persist) { File.WriteAllText(StorePath,JsonUtility.ToJson(plan,true)); if(image) File.WriteAllBytes(Path.Combine(Application.persistentDataPath,"imported-plan.png"),image.EncodeToPNG()); }
        }
        // Install(plan,select): สร้าง GameObject ของห้องทั้งหมดและลงทะเบียนเป็นแผนที่ใน StartScreen
        void Install(Plan plan,bool select)
        {
            // room = RoomSandbox ของผู้เล่น (ตัวแปรนี้ไม่ได้ถูกใช้ต่อในฟังก์ชัน)
            var room=Flow.Player.Room;
            // ถ้ายังไม่มีวัสดุ สร้างวัสดุกำแพง (สีครีมเทา) และพื้น (สีน้ำตาลไม้)
            if(!wallMaterial) { wallMaterial=new Material(Shader.Find("Standard")); wallMaterial.color=new Color(.82f,.82f,.77f); floorMaterial=new Material(Shader.Find("Standard")); floorMaterial.color=new Color(.49f,.43f,.34f); }
            // สร้าง GameObject ราก 'Imported floor plan' ใต้โลกภายใน (Interior)
            var root=new GameObject("Imported floor plan"); root.transform.SetParent(Flow.Interior.transform,false);
            // ติด SandboxMap ให้ราก: Id=key ของแปลน, ชื่อ 'Imported Plan', คำอธิบายบอกขนาด, HalfSize จากขนาดแปลน, Preview=รูปแปลน
            var map=root.AddComponent<SandboxMap>(); map.Id=plan.key; map.Title="Imported Plan"; map.Description=plan.width.ToString("0.0")+" x "+plan.depth.ToString("0.0")+" m / image draft"; map.HalfSize=new Vector2(plan.width/2,plan.depth/2); map.Preview=image;
            // สร้าง GameObject แม่สำหรับเฟอร์นิเจอร์ของแผนที่นี้ ผูกเป็นลูกของราก และตั้งเป็น map.Furniture
            map.Furniture=new GameObject("Imported plan furniture").transform; map.Furniture.SetParent(root.transform,false);
            // สร้างพื้นห้อง (แผ่นหนา 0.2 ม. จมใต้ y=0 เล็กน้อย) ขนาดเท่าแปลน ไม่เป็นสิ่งกีดขวาง
            Make(root,"Floor",new Vector3(0,-.1f,0),new Vector3(plan.width,.2f,plan.depth),floorMaterial,false);
            // สร้างกำแพงทุกชิ้นที่ตรวจจับได้ (สูง 2.8 ม.) เป็นสิ่งกีดขวาง
            foreach(var r in plan.walls) Make(root,"Detected wall",new Vector3(r.center.x,1.4f,r.center.y),new Vector3(r.width,2.8f,r.height),wallMaterial,true);
            // ถ้ามีหน้าต่าง:
            if(plan.windows!=null && plan.windows.Count>0)
            {
                // ถ้ายังไม่มีวัสดุกรอบ/กระจก ให้สร้าง:
                if(!frameMaterial)
                {
                    // วัสดุกรอบ (สีเทาเข้ม)
                    frameMaterial=new Material(Shader.Find("Standard")); frameMaterial.color=new Color(.12f,.15f,.17f);
                    // วัสดุกระจก (สีฟ้าโปร่ง alpha 0.25)
                    glassMaterial=new Material(Shader.Find("Standard")); glassMaterial.color=new Color(.55f,.82f,.9f,.25f);
                    // ตั้งโหมดโปร่งใสของ Standard shader (_Mode=3) พร้อมโหมดผสมสี One/OneMinusSrcAlpha (premultiplied alpha)
                    glassMaterial.SetFloat("_Mode",3); glassMaterial.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
                    // (ต่อ) ตั้งตัวคูณผสมปลายทาง
                    glassMaterial.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    // ปิดการเขียน Z-buffer, เปิด keyword _ALPHAPREMULTIPLY_ON, ตั้ง renderQueue=3000 (คิวโปร่งใส)
                    glassMaterial.SetInt("_ZWrite",0); glassMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON"); glassMaterial.renderQueue=3000;
                }
                // สร้างโมเดลหน้าต่างทุกบาน (FloorPlanWindows.Build)
                foreach(var r in plan.windows) FloorPlanWindows.Build(root,r,wallMaterial,frameMaterial,glassMaterial);
            }
            // คำนวณขอบเขตเดินได้ (convex hull) จากกำแพง เก็บใน map.WalkBoundary
            map.WalkBoundary=PlanWalkBoundary.Build(plan.walls);
            // สร้างประตูทุกบานที่ผู้ใช้ยืนยันแล้ว (วัสดุบานใช้สีพื้น, วัสดุวงกบใช้สีกำแพง)
            if(plan.doors!=null) foreach(var door in plan.doors) if(door.confirmed) PlanDoor.Build(root,door,floorMaterial,wallMaterial,Flow.Player);
            // หาจุดเกิดที่ปลอดภัย (found = หาเจอหรือไม่)
            bool found=PlanWalkBoundary.TrySpawn(plan,map.WalkBoundary,out var safeSpawn);
            // ตั้งจุดเกิดของแผนที่
            map.Spawn=safeSpawn;
            // ถ้าหาจุดเกิดไม่เจอ ทำลายห้องที่สร้างไปแล้วและโยนข้อผิดพลาด
            if(!found) { Destroy(root); throw new ArgumentException("No safe player spawn found. Adjust detection."); }
            // ซ่อนห้องนี้ไว้ก่อน (StartScreen จะเปิดเมื่อผู้เล่นเลือกแผนที่)
            root.SetActive(false);
            // list = สำเนารายการแผนที่เดิม; index = ลำดับของแผนที่ที่เป็นแปลนนำเข้า (Id ขึ้นต้น 'plan-') ที่มีอยู่แล้ว (-1 ถ้าไม่มี)
            var list=new List<SandboxMap>(Flow.Maps); int index=list.FindIndex(m=>m.Id.StartsWith("plan-"));
            // ถ้ามีแผนที่แปลนเดิมอยู่:
            if(index>=0)
            {
                // หมายเหตุเดิม: ไม่ยอมทิ้งเลย์เอาต์ที่ผู้เล่นแก้ไขไปแล้วโดยเงียบๆ
                // Refuse to silently discard a layout the player has already edited.
                // ถ้าเป็นการสร้างจริง (select) และแผนที่เดิมมีเฟอร์นิเจอร์อยู่ → ทำลายห้องใหม่ แล้วโยนข้อผิดพลาดให้บันทึกและเอาชิ้นออกก่อน
                if(select && list[index].Furniture.childCount>0) { Destroy(root); throw new InvalidOperationException("Imported map contains furniture. Save it, then remove those pieces before replacing the plan."); }
                // ซ่อนและทำลายแผนที่เดิม แล้วแทนที่ด้วยแผนที่ใหม่ในลิสต์
                list[index].gameObject.SetActive(false); Destroy(list[index].gameObject); list[index]=map;
            }
            // ถ้ายังไม่มีแผนที่แปลน: เพิ่มต่อท้ายลิสต์ index = ลำดับสุดท้าย
            else { index=list.Count; list.Add(map); }
            // อัปเดตอาร์เรย์ Maps ของ StartScreen
            Flow.Maps=list.ToArray();
            // ถ้า select=true เลือกแผนที่นี้ทันที
            if(select) Flow.SelectMap(index);
            // สร้างปุ่ม 3 มิติสำหรับเลือกแผนที่ที่นำเข้า (ใช้ใน VR)
            AddVRButton(index);
        }
        // AddVRButton(index): สร้าง/แทนที่ปุ่ม 'SELECT IMPORTED PLAN' ในฉากต้อนรับสำหรับ VR
        void AddVRButton(int index)
        {
            // ถ้ามีปุ่มเดิมอยู่แล้ว ทำลายทิ้ง
            var old=Flow.Welcome.transform.Find("Select imported plan"); if(old) Destroy(old.gameObject);
            // สร้างลูกบาศก์ 'Select imported plan' เป็นลูกของฉากต้อนรับ ตั้งตำแหน่ง ขนาดเป็นแผ่นแบนกว้าง และวัสดุเป็นสีกำแพง
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="Select imported plan"; go.transform.SetParent(Flow.Welcome.transform,false); go.transform.localPosition=new Vector3(0,1.4f,-.06f); go.transform.localScale=new Vector3(3,.27f,.03f); go.GetComponent<Renderer>().sharedMaterial=wallMaterial;
            // ติด RoomButton: Command=11 (เลือกแผนที่), ItemId=index
            var b=go.AddComponent<RoomButton>(); b.Command=11; b.ItemId=index;
            // สร้างวัตถุป้ายข้อความเป็นลูกของปุ่ม — ตั้งสเกลผกผัน เพื่อหักล้างสเกลแบนของปุ่มแม่ ให้ตัวอักษรไม่เพี้ยน — และตำแหน่ง
            var label=new GameObject("Imported plan label"); label.transform.SetParent(go.transform,false); label.transform.localScale=new Vector3(1/3f,1/.27f,1/.03f); label.transform.localPosition=new Vector3(-.45f,.25f,-.6f);
            // เพิ่ม TextMesh ข้อความ 'SELECT IMPORTED PLAN' สีดำ ฟอนต์ 48 ขนาดตัวอักษร 0.012
            var text=label.AddComponent<TextMesh>(); text.text="SELECT IMPORTED PLAN"; text.color=Color.black; text.fontSize=48; text.characterSize=.012f;
        }
        // Make(...): ฟังก์ชันช่วยสร้างกล่องของห้อง (พื้น/กำแพง)
        static void Make(GameObject root,string name,Vector3 pos,Vector3 size,Material mat,bool obstacle)
        // สร้างด้วย FurnitureCatalog.Part, เปิด Collider, และถ้า obstacle=true ติด RoomObstacle
        { var go=FurnitureCatalog.Part(root,name,pos,size,mat); go.GetComponent<Collider>().enabled=true; if(obstacle) go.AddComponent<RoomObstacle>(); }
        // ReadFolder(): อ่านรายการโฟลเดอร์ย่อยและรูปในโฟลเดอร์ปัจจุบัน
        void ReadFolder()
        {
            // directories = โฟลเดอร์ย่อยทั้งหมด; result = ลิสต์เก็บรูปที่เข้าเกณฑ์
            directories=Directory.GetDirectories(folder); var result=new List<string>();
            // วนไฟล์ในโฟลเดอร์ เก็บเฉพาะนามสกุล .png/.jpg/.jpeg
            foreach(var file in Directory.GetFiles(folder)) { string ext=Path.GetExtension(file).ToLowerInvariant(); if(ext==".png" || ext==".jpg" || ext==".jpeg") result.Add(file); }
            // แปลงลิสต์เป็นอาร์เรย์เก็บใน files
            files=result.ToArray();
        }
        // OnGUI(): วาดหน้าต่างนำเข้าแปลนทั้งหมดด้วย IMGUI
        void OnGUI()
        {
            // ถ้าเมนูหลักไม่ได้เปิดอยู่ หรือใช้ VR ไม่วาด
            if(!Flow.IsOpen || XRSettings.isDeviceActive) return;
            // ถ้าหน้าต่างนำเข้าไม่ได้เปิด ไม่วาด
            if(!open) return;
            // ตั้งความลึกการวาดเป็น -10 ให้อยู่เหนือ UI อื่น
            GUI.depth=-10;
            // เก็บ matrix เดิม ตั้งสเกลให้เลย์เอาต์ 1100×760 พอดีกับจอ
            var old=GUI.matrix; float scale=Mathf.Min(Screen.width/1100f,Screen.height/760f); GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            // วาดพื้นหลังหน้าต่าง (สี่เหลี่ยมสีเข้ม) แล้วคืนสีเดิม
            var backdrop=GUI.color; GUI.color=new Color(.09f,.13f,.14f); GUI.DrawTexture(new Rect(20,65,1060,675),Texture2D.whiteTexture); GUI.color=backdrop;
            // วาดกรอบหน้าต่างพร้อมหัวข้อ
            GUI.Box(new Rect(20,65,1060,675),"IMPORT FLOOR PLAN / orthogonal dark wall lines");
            // ช่องกรอกพาธไฟล์รูป
            filePath=GUI.TextField(new Rect(40,100,750,28),filePath);
            // ปุ่ม 'Load path' โหลดรูปจากพาธที่กรอก (ครอบด้วย Attempt เพื่อดักข้อผิดพลาดเป็นข้อความสถานะ)
            if(GUI.Button(new Rect(800,100,110,28),"Load path")) Attempt(()=>LoadImageFile(filePath));
            // ปุ่ม 'Browse images' อ่านโฟลเดอร์แล้วเปิดหน้าต่างเลือกไฟล์
            if(GUI.Button(new Rect(920,100,130,28),"Browse images")) Attempt(()=>{ReadFolder();browse=true;});
            // ถ้ามีรูป วาดภาพแปลนในกรอบ 650×440 ที่ตำแหน่ง (40,150) โดยใช้เฉพาะส่วนที่ครอป (imageCrop)
            if(image) GUI.DrawTextureWithTexCoords(new Rect(40,150,650,440),image,imageCrop);
            // ถ้ามีฉบับร่าง วนทุกกำแพง:
            if(draft!=null) foreach(var r in draft.walls)
            {
                // ตั้งสีแดงโปร่งใส
                Color before=GUI.color; GUI.color=new Color(1,.15f,.05f,.6f);
                // วาดสี่เหลี่ยมกำแพงทับรูป: แปลงพิกัดเมตร → พิกเซลในกรอบ (กลับแกน y เพราะ GUI นับจากบน) ความสูงขั้นต่ำ 2 พิกเซล แล้วคืนสีเดิม
                GUI.DrawTexture(new Rect(40+(r.x+draft.width/2)/draft.width*650,150+(1-(r.y+r.height+draft.depth/2)/draft.depth)*440,r.width/draft.width*650,Mathf.Max(2,r.height/draft.depth*440)),Texture2D.whiteTexture); GUI.color=before;
            }
            // ถ้ามีหน้าต่าง วนทุกบาน:
            if(draft!=null && draft.windows!=null) foreach(var r in draft.windows)
            {
                // วาดสี่เหลี่ยมหน้าต่างสีฟ้า (cyan) ทับรูป ด้วย PreviewRect แล้วคืนสี
                var before=GUI.color; GUI.color=new Color(0,.85f,1,.85f); GUI.DrawTexture(PreviewRect(r),Texture2D.whiteTexture); GUI.color=before;
            }
            // ถ้ามีฉบับร่างและไม่ได้เปิดหน้าเลือกไฟล์ วาดตัวแก้ไขหน้าต่าง
            if(draft!=null && !browse) WindowEditor();
            // และวาดตัวแก้ไขประตู
            if(draft!=null && !browse) DoorEditor();
            // ป้าย 'Cropped PLAN width (metres)'
            GUI.Label(new Rect(720,150,320,25),"Cropped PLAN width (metres)");
            // ช่องกรอกความกว้างแปลน
            widthText=GUI.TextField(new Rect(720,178,110,27),widthText);
            // ปุ่ม 'Apply' (ครอบด้วย Attempt):
            if(GUI.Button(new Rect(838,178,62,27),"Apply")) Attempt(()=>{
                // แปลงข้อความเป็นตัวเลข (รูปแบบสากล InvariantCulture); ถ้าแปลงไม่ได้ โยนข้อผิดพลาด
                if(!float.TryParse(widthText,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float typed)) throw new ArgumentException("Enter a width such as 12.50 metres.");
                // ปรับความกว้างแปลนตามที่กรอก
                SetPlanWidth(typed);
            // ปิดฟังก์ชันนิรนามที่ส่งให้ Attempt และปิดการเรียก GUI.Button
            });
            // ปุ่ม '-10%' ลดความกว้างลง 10%
            if(GUI.Button(new Rect(908,178,59,27),"-10%")) Attempt(()=>SetPlanWidth(width*.9f));
            // ปุ่ม '+10%' เพิ่มความกว้างขึ้น 10%
            if(GUI.Button(new Rect(975,178,59,27),"+10%")) Attempt(()=>SetPlanWidth(width*1.1f));
            // aspect = สัดส่วนความลึกต่อความกว้างของภาพที่ครอป
            float aspect=image?image.height*imageCrop.height/(image.width*imageCrop.width):1;
            // ป้ายแสดงขนาด กว้าง × ลึก (เมตร) และพื้นที่ (ตร.ม.)
            GUI.Label(new Rect(720,209,320,35),width.ToString("0.00")+" x "+(width*aspect).ToString("0.00")+" m | footprint "+(width*width*aspect).ToString("0.0")+" m2");
            // ป้ายค่าเกณฑ์ความมืดของพิกเซล
            GUI.Label(new Rect(720,250,320,30),"Dark-pixel threshold: "+threshold.ToString("0.00"));
            // แถบเลื่อนปรับเกณฑ์ (0.05–0.75); ถ้าค่าเปลี่ยน เก็บค่าและล้าง draft (ต้องตรวจจับใหม่)
            float value=GUI.HorizontalSlider(new Rect(720,285,310,25),threshold,.05f,.75f); if(value!=threshold) { threshold=value;draft=null; }
            // ป้ายความยาวกำแพงขั้นต่ำ
            GUI.Label(new Rect(720,325,320,30),"Minimum wall length: "+minWall.ToString("0.00")+" m");
            // แถบเลื่อนความยาวขั้นต่ำ (0.3–2 ม.); เปลี่ยนแล้วล้าง draft
            value=GUI.HorizontalSlider(new Rect(720,360,310,25),minWall,.3f,2); if(value!=minWall) { minWall=value;draft=null; }
            // ปุ่ม '1. DETECT / PREVIEW WALLS' เรียก PreviewCurrent
            if(GUI.Button(new Rect(720,405,310,40),"1. DETECT / PREVIEW WALLS")) Attempt(()=>PreviewCurrent());
            // ปิดปุ่มถัดไปถ้ายังไม่มี draft
            GUI.enabled=draft!=null;
            // ปุ่ม '2. GENERATE MAP' สร้างแผนที่จาก draft แล้วปิดหน้าต่าง
            if(GUI.Button(new Rect(720,462,310,45),"2. GENERATE MAP")) Attempt(()=>{Generate(draft);open=false;});
            // เปิดการใช้งานปุ่มกลับเป็นปกติ
            GUI.enabled=true;
            // คำอธิบายสี: ส้ม = ข้อเสนอ, เขียว = ยอมรับแล้ว; ตั้งบานพับ/ทิศเหวี่ยง/ความกว้างประตู แล้วกด Accept; ช่องที่กว้างเกินจะเติมด้วยกำแพง
            GUI.Label(new Rect(720,530,320,58),"ORANGE suggestion / GREEN accepted\nSet hinge, swing and door width, then Accept.\nExtra opening width is filled with wall.");
            // แสดงข้อความสถานะด้านล่าง
            GUI.Label(new Rect(40,688,840,45),status);
            // ปุ่ม 'CLOSE' ปิดหน้าต่างนำเข้า
            if(GUI.Button(new Rect(900,680,150,35),"CLOSE")) open=false;
            // ถ้าอยู่โหมดเลือกไฟล์ วาดหน้าต่างเลือกไฟล์ทับ:
            if(browse)
            {
                // กรอบ 'Choose image' และช่องกรอกโฟลเดอร์
                GUI.Box(new Rect(35,140,1025,535),"Choose image"); folder=GUI.TextField(new Rect(55,175,800,28),folder);
                // ปุ่ม 'Go' อ่านโฟลเดอร์ที่พิมพ์
                if(GUI.Button(new Rect(870,175,80,28),"Go")) Attempt(ReadFolder);
                // ปุ่ม 'Up' ขึ้นไปโฟลเดอร์แม่ แล้วอ่านรายการใหม่
                if(GUI.Button(new Rect(960,175,80,28),"Up")) Attempt(()=>{folder=Directory.GetParent(folder)?.FullName??folder;ReadFolder();});
                // เริ่มพื้นที่เลื่อน (ความสูงเนื้อหา = 32 พิกเซล × (จำนวนโฟลเดอร์ + ไฟล์)); row = ลำดับแถว
                scroll=GUI.BeginScrollView(new Rect(55,220,980,390),scroll,new Rect(0,0,940,(directories.Length+files.Length)*32)); int row=0;
                // แสดงปุ่มโฟลเดอร์ย่อยแต่ละอัน '[Folder] ชื่อ' — กดแล้วเข้าไปในโฟลเดอร์นั้น (break เพราะรายการเปลี่ยน)
                foreach(string dir in directories) { if(GUI.Button(new Rect(0,row++*32,920,30),"[Folder] "+Path.GetFileName(dir))) { string chosen=dir;Attempt(()=>{folder=chosen;ReadFolder();});break; } }
                // แสดงปุ่มไฟล์รูปแต่ละไฟล์ — กดแล้วโหลดรูปนั้นและปิดหน้าต่างเลือกไฟล์
                foreach(string file in files) { if(GUI.Button(new Rect(0,row++*32,920,30),Path.GetFileName(file))) { string chosen=file;Attempt(()=>{LoadImageFile(chosen);browse=false;});break; } }
                // จบพื้นที่เลื่อน และปุ่ม 'Back' กลับ
                GUI.EndScrollView(); if(GUI.Button(new Rect(880,625,150,30),"Back")) browse=false;
            }
            // คืน matrix เดิมและความลึกการวาดเป็น 0
            GUI.matrix=old; GUI.depth=0;
        }
        // Attempt(action): รันการกระทำใน try/catch ถ้าพลาดเก็บข้อความข้อผิดพลาดเป็นสถานะ (ไม่ให้เกมล่ม)
        void Attempt(Action action) { try { action(); } catch(Exception e) { status=e.Message; } }
        // PreviewRect(r): แปลงสี่เหลี่ยมหน่วยเมตรเป็นสี่เหลี่ยมพิกเซลบนกรอบพรีวิว (กลับแกน y, ขนาดขั้นต่ำ 3 พิกเซล)
        Rect PreviewRect(Rect r) => new Rect(40+(r.x+draft.width/2)/draft.width*650,150+(1-(r.y+r.height+draft.depth/2)/draft.depth)*440,Mathf.Max(3,r.width/draft.width*650),Mathf.Max(3,r.height/draft.depth*440));
        // WindowEditor(): ตัวแก้ไขหน้าต่าง (คลิก 2 ครั้งเพื่อเพิ่ม คลิกขวาลบ)
        void WindowEditor()
        {
            // ช่องติ๊ก 'EDIT WINDOWS (2 clicks)'
            bool next=GUI.Toggle(new Rect(40,590,220,24),editWindows,"EDIT WINDOWS (2 clicks)");
            // ถ้าค่าเปลี่ยน: เก็บค่า ล้างจุดคลิกค้าง และถ้าเปิดโหมดนี้ให้ปิดโหมดแก้ประตูกันชน
            if(next!=editWindows) { editWindows=next;windowStart=null;if(next) { editDoors=false;doorStart=null; } }
            // ปุ่ม 'Clear windows' ลบหน้าต่างทั้งหมด ล้างจุดค้าง และอัปเดต key
            if(GUI.Button(new Rect(270,590,160,24),"Clear windows")) { draft.windows.Clear();windowStart=null;FloorPlanDetector.UpdateKey(draft); }
            // ปุ่ม 'Cancel pending point' ยกเลิกจุดที่คลิกค้างไว้
            if(GUI.Button(new Rect(440,590,245,24),"Cancel pending point")) windowStart=null;
            // ถ้ามีจุดคลิกแรกค้างอยู่:
            if(windowStart.HasValue)
            {
                // ตั้งสีฟ้า (cyan)
                Vector2 p=windowStart.Value; var before=GUI.color; GUI.color=Color.cyan;
                // วาดจุดสี่เหลี่ยมเล็กตรงตำแหน่งที่คลิก แล้วคืนสี
                GUI.DrawTexture(new Rect(p.x-4,p.y-4,8,8),Texture2D.whiteTexture);GUI.color=before;
            }
            // e = เหตุการณ์ IMGUI ปัจจุบัน
            var e=Event.current;
            // ออกถ้าไม่ได้อยู่โหมดแก้ไข หรือไม่ใช่การกดเมาส์ หรือเมาส์อยู่นอกกรอบพรีวิว
            if(!editWindows || e.type!=EventType.MouseDown || !new Rect(40,150,650,440).Contains(e.mousePosition)) return;
            // ถ้าเป็นคลิกขวา (ลบ):
            if(e.button==1)
            {
                // วนหน้าต่างจากท้าย หาอันที่ถูกคลิก (ขยายพื้นที่คลิก 5 พิกเซล) แล้วลบ พร้อมอัปเดต key
                for(int i=draft.windows.Count-1;i>=0;i--) { var hit=PreviewRect(draft.windows[i]);hit.xMin-=5;hit.xMax+=5;hit.yMin-=5;hit.yMax+=5;if(hit.Contains(e.mousePosition)) { draft.windows.RemoveAt(i);FloorPlanDetector.UpdateKey(draft);break; } }
                // ล้างจุดค้าง, ใช้เหตุการณ์ (e.Use() กันไม่ให้ UI อื่นรับซ้ำ) แล้วออก
                windowStart=null;e.Use();return;
            }
            // ถ้าไม่ใช่คลิกซ้าย ออก
            if(e.button!=0) return;
            // คลิกแรก: จำจุดไว้และแจ้งให้คลิกปลายอีกด้าน
            if(!windowStart.HasValue) { windowStart=e.mousePosition; status="Click the other end of the window. Right-click cyan to remove. Detect resets edits."; }
            // คลิกที่สอง:
            else
            {
                // แปลงจุดคลิกทั้งสองเป็นพิกัดแปลน (เมตร) และล้างจุดค้าง
                Vector2 a=ToPlan(windowStart.Value),b=ToPlan(e.mousePosition);windowStart=null;
                // horizontal = แนวนอนถ้าระยะแกน x ≥ แกน y
                bool horizontal=Mathf.Abs(b.x-a.x)>=Mathf.Abs(b.y-a.y);
                // length = ความยาวตามแนวนั้น
                float length=horizontal?Mathf.Abs(b.x-a.x):Mathf.Abs(b.y-a.y);
                // ถ้ายาว ≥ 0.2 ม. และยังมีหน้าต่างไม่ถึง 100:
                if(length>=.2f && draft.windows.Count<100)
                {
                    // สร้างสี่เหลี่ยมหน้าต่าง (หนา 0.12 ม.) จากสองจุด — ตำแหน่งตั้งฉากบีบให้อยู่ในแปลน
                    Rect r=horizontal?new Rect(Mathf.Min(a.x,b.x),Mathf.Clamp(a.y,-draft.depth/2+.06f,draft.depth/2-.06f)-.06f,length,.12f):new Rect(Mathf.Clamp(a.x,-draft.width/2+.06f,draft.width/2-.06f)-.06f,Mathf.Min(a.y,b.y),.12f,length);
                    // ตรวจว่าซ้อนกับกำแพงหรือไม่
                    bool overlap=false;foreach(var wall in draft.walls) if(wall.Overlaps(r)) { overlap=true;break; }
                    // ตรวจว่าซ้อนกับประตูที่ยืนยันแล้วหรือไม่
                    if(draft.doors!=null) foreach(var door in draft.doors) if(door.confirmed && door.opening.Overlaps(r)) overlap=true;
                    // ถ้าซ้อน แจ้งให้เลือกจุดในช่องว่าง
                    if(overlap) status="Window overlaps a red wall. Select endpoints inside the empty opening.";
                    // ไม่ซ้อน: เพิ่มหน้าต่าง อัปเดต key และแจ้งสถานะ
                    else { draft.windows.Add(r);FloorPlanDetector.UpdateKey(draft);status="Window added. Cyan windows generate frames, glass and sill walls."; }
                }
            }
            // ใช้เหตุการณ์เมาส์นี้ (กัน UI อื่นรับซ้ำ)
            e.Use();
        }
        // DoorEditor(): ตัวแก้ไขประตู (วาดประตูทั้งหมด, เลือก/เพิ่ม/ลบ/สลับบานพับ/กลับทิศ/ปรับความกว้าง/ยืนยัน)
        void DoorEditor()
        {
            // ถ้ารายการประตูเป็น null สร้างลิสต์ว่าง
            if(draft.doors==null) draft.doors=new List<PlanDoorData>();
            // วนวาดประตูทุกบานบนพรีวิว:
            for(int i=0;i<draft.doors.Count;i++)
            {
                // d = ประตู; ตั้งสี: เหลือง = เลือกอยู่, เขียว = ยืนยันแล้ว, ส้ม = ข้อเสนอ
                var d=draft.doors[i];var before=GUI.color;GUI.color=i==selectedDoor?Color.yellow:(d.confirmed?Color.green:new Color(1,.55f,.05f));
                // วาดสี่เหลี่ยมช่องเดินผ่าน (Passage)
                GUI.DrawTexture(PreviewRect(d.Passage),Texture2D.whiteTexture);
                // วาดจุดบานพับเป็นสี่เหลี่ยมเล็ก
                var h=ToPreview(d.Hinge);GUI.DrawTexture(new Rect(h.x-4,h.y-4,8,8),Texture2D.whiteTexture);
                // วาดเส้นบานประตูตอนเปิด 90° จากบานพับ
                DrawLine(h,ToPreview(d.BuiltArc(Mathf.PI/2)));
                // วาดเส้นโค้งการเหวี่ยงด้วยเส้นตรงสั้นๆ 20 ช่วง ตั้งแต่มุม 0 ถึง 90°
                for(int s=0;s<20;s++) DrawLine(ToPreview(d.BuiltArc(s*Mathf.PI/40)),ToPreview(d.BuiltArc((s+1)*Mathf.PI/40)));
                // คืนสีเดิม
                GUI.color=before;
            }
            // ช่องติ๊ก 'EDIT DOORS (2 clicks)'
            bool next=GUI.Toggle(new Rect(40,620,220,24),editDoors,"EDIT DOORS (2 clicks)");
            // ถ้าค่าเปลี่ยน: เก็บค่า ล้างจุดค้าง และถ้าเปิดให้ปิดโหมดแก้หน้าต่าง
            if(next!=editDoors) { editDoors=next;doorStart=null;if(next) { editWindows=false;windowStart=null; } }
            // เปิดใช้ปุ่มด้านล่างเฉพาะเมื่อมีประตูที่เลือกอยู่
            GUI.enabled=selectedDoor>=0 && selectedDoor<draft.doors.Count;
            // ถ้ามีประตูที่เลือก:
            if(GUI.enabled)
            {
                // d = ประตูที่เลือก
                var d=draft.doors[selectedDoor];
                // ป้ายความกว้างช่องกรอบประตู
                GUI.Label(new Rect(720,590,310,28),"Door frame width: "+d.PassageWidth.ToString("0.00")+" m");
                // แถบเลื่อนปรับความกว้างช่องกรอบ (0.7–1.6 ม. แต่ไม่เกินความยาวช่องเปิด)
                float size=GUI.HorizontalSlider(new Rect(720,622,310,20),d.PassageWidth,Mathf.Min(.7f,d.Length),Mathf.Min(1.6f,d.Length));
                // ถ้าค่าเปลี่ยน (เกิน 1 มม.) บันทึกค่าใหม่ และอัปเดต key
                if(Mathf.Abs(size-d.PassageWidth)>.001f) { d.passageWidth=size;FloorPlanDetector.UpdateKey(draft); }
            }
            // ปุ่ม 'Swap hinge' สลับด้านบานพับ
            if(GUI.Button(new Rect(270,620,135,26),"Swap hinge")) { var d=draft.doors[selectedDoor];d.hingeAtEnd=!d.hingeAtEnd;FloorPlanDetector.UpdateKey(draft); }
            // ปุ่ม 'Reverse swing' กลับด้านที่บานเหวี่ย
            if(GUI.Button(new Rect(415,620,135,26),"Reverse swing")) { var d=draft.doors[selectedDoor];d.positiveSide=!d.positiveSide;FloorPlanDetector.UpdateKey(draft); }
            // ปุ่ม 'Remove door' ลบประตูที่เลือกและยกเลิกการเลือก
            if(GUI.Button(new Rect(560,620,125,26),"Remove door")) { draft.doors.RemoveAt(selectedDoor);selectedDoor=-1;FloorPlanDetector.UpdateKey(draft); }
            // ปุ่ม 'Accept selected door' ยืนยันประตูที่เลือก (confirmed=true) ให้ถูกสร้างเมื่อกด Generate
            if(GUI.Button(new Rect(720,646,310,28),"Accept selected door")) { if(selectedDoor>=0 && selectedDoor<draft.doors.Count) { draft.doors[selectedDoor].confirmed=true;FloorPlanDetector.UpdateKey(draft);status="Door accepted (green). It will be included when you Generate."; } }
            // เปิดใช้งานปุ่มกลับเป็นปกติ
            GUI.enabled=true;
            // ข้อความแนะนำการใช้งาน
            GUI.Label(new Rect(40,651,650,35),"Select orange or click 2 gap endpoints. Right-click cancels. Detect resets edits.");
            // ถ้ามีจุดคลิกแรกค้างอยู่ วาดกรอบสี่เหลี่ยมเล็กตรงจุดนั้น
            if(doorStart.HasValue) { var p=doorStart.Value;GUI.Box(new Rect(p.x-4,p.y-4,8,8),""); }
            // e = เหตุการณ์ IMGUI ปัจจุบัน
            var e=Event.current;
            // ออกถ้าไม่ได้อยู่โหมดแก้ไข หรือไม่ใช่การกดเมาส์ หรือเมาส์อยู่นอกกรอบพรีวิว
            if(!editDoors || e.type!=EventType.MouseDown || !new Rect(40,150,650,440).Contains(e.mousePosition)) return;
            // คลิกขวา: ยกเลิกจุดที่ค้าง แล้วออก
            if(e.button==1) { doorStart=null;e.Use();return; }
            // ถ้าไม่ใช่คลิกซ้าย ออก
            if(e.button!=0) return;
            // คลิกแรก:
            if(!doorStart.HasValue)
            {
                // ถ้าคลิกโดนช่องเปิดของประตูที่มีอยู่ (ขยายพื้นที่คลิก 6 พิกเซล) → เลือกประตูนั้นแล้วออก
                for(int i=0;i<draft.doors.Count;i++) { var hit=PreviewRect(draft.doors[i].opening);hit.xMin-=6;hit.xMax+=6;hit.yMin-=6;hit.yMax+=6;if(hit.Contains(e.mousePosition)) { selectedDoor=i;e.Use();return; } }
                // ไม่โดนประตูใด → จำเป็นจุดเริ่มของประตูใหม่
                doorStart=e.mousePosition;
            }
            // คลิกที่สอง:
            else
            {
                // แปลงจุดคลิกทั้งสองเป็นพิกัดแปลน และล้างจุดค้าง
                var a=ToPlan(doorStart.Value);var b=ToPlan(e.mousePosition);doorStart=null;
                // horizontal = แนวนอนถ้าระยะแกน x ≥ แกน y
                bool horizontal=Mathf.Abs(b.x-a.x)>=Mathf.Abs(b.y-a.y);
                // length = ความยาวช่องตามแนวนั้น
                float length=horizontal?Mathf.Abs(b.x-a.x):Mathf.Abs(b.y-a.y);
                // สร้างสี่เหลี่ยมช่องเปิด (หนา 0.08 ม.) ตามสองจุด — ตำแหน่งตั้งฉากบีบให้อยู่ในแปลน
                var opening=horizontal?new Rect(Mathf.Min(a.x,b.x),Mathf.Clamp(a.y,-draft.depth/2+.04f,draft.depth/2-.04f)-.04f,length,.08f):new Rect(Mathf.Clamp(a.x,-draft.width/2+.04f,draft.width/2-.04f)-.04f,Mathf.Min(a.y,b.y),.08f,length);
                // blocked = ซ้อนกับกำแพง?
                bool blocked=false;foreach(var r in draft.walls) if(r.Overlaps(opening)) blocked=true;
                // หรือซ้อนกับหน้าต่าง?
                foreach(var r in draft.windows) if(r.Overlaps(opening)) blocked=true;
                // หรือซ้อนกับประตูที่มีอยู่?
                foreach(var d in draft.doors) if(d.opening.Overlaps(opening)) blocked=true;
                // ถ้ายาวไม่อยู่ใน 0.4–4 ม., มีประตูครบ 60 บานแล้ว, หรือถูกบล็อก แจ้งให้เลือกช่องว่างที่เหมาะสม
                if(length<.4f || length>4 || draft.doors.Count>=60 || blocked) status="Choose an empty gap 0.4-4 m wide, clear of red walls and cyan windows.";
                // ไม่งั้นเพิ่มประตูใหม่ (บานพับอยู่ปลายที่คลิกแรก: hingeAtEnd=true เมื่อลากจากค่ามากไปหาค่าน้อย) เลือกประตูนั้น อัปเดต key และแจ้งสถานะ
                else { draft.doors.Add(new PlanDoorData{opening=opening,hingeAtEnd=horizontal?b.x<a.x:b.y<a.y});selectedDoor=draft.doors.Count-1;FloorPlanDetector.UpdateKey(draft);status="Door added. Check hinge / swing, then Accept selected door."; }
            }
            // ใช้เหตุการณ์เมาส์นี้ (กัน UI อื่นรับซ้ำ)
            e.Use();
        }
        // ToPreview(p): แปลงจุดหน่วยเมตร (กลางแปลนเป็นศูนย์) เป็นพิกเซลบนกรอบพรีวิว (กลับแกน y)
        Vector2 ToPreview(Vector2 p) => new Vector2(40+(p.x/draft.width+.5f)*650,150+(.5f-p.y/draft.depth)*440);
        // DrawLine(a,b): วาดเส้นตรงจากจุด a ถึง b บนหน้าจอ IMGUI (ใช้การหมุน matrix รอบ a แล้ววาดแถบบาง 2 พิกเซล)
        static void DrawLine(Vector2 a,Vector2 b)
        // เก็บ matrix เดิม; หมุนรอบ a ตามมุมเส้น; วาดสี่เหลี่ยมยาวเท่าระยะ a→b สูง 2 พิกเซล; คืน matrix
        { var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,(b-a).magnitude,2),Texture2D.whiteTexture);GUI.matrix=matrix; }
        // ToPlan(p): แปลงพิกเซลบนกรอบพรีวิวกลับเป็นพิกัดแปลนหน่วยเมตร (ผกผันของ ToPreview)
        Vector2 ToPlan(Vector2 p) => new Vector2(((p.x-40)/650-.5f)*draft.width,(.5f-(p.y-150)/440)*draft.depth);
        // OnDestroy(): ทำลายวัสดุทั้งสี่และรูปที่คลาสนี้สร้างไว้ ป้องกันหน่วยความจำรั่ว
        void OnDestroy() { if(wallMaterial) Destroy(wallMaterial); if(floorMaterial) Destroy(floorMaterial);if(frameMaterial) Destroy(frameMaterial);if(glassMaterial) Destroy(glassMaterial); foreach(var texture in ownedImages) if(texture) Destroy(texture); }
    }
}

