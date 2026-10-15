// ==== RoomSandbox.cs ====
// หัวใจของระบบจัดห้อง: เก็บรายการเฟอร์นิเจอร์ (Items), ชิ้นที่กำลังถือ (Held), สร้าง/หยิบ/ย้าย/หมุน/วาง/ยกเลิก/ลบ/ล้าง,
// ตรวจความถูกต้องของตำแหน่ง (IsValid), คิดราคารวม, อัปเดตข้อความสถานะ, และรับคำสั่งจากปุ่ม 3 มิติ (Command)
// นำเข้า System.Collections.Generic — ใช้ List<T>
using System.Collections.Generic;
// นำเข้า UnityEngine
using UnityEngine;

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส RoomSandbox (MonoBehaviour)
    public sealed class RoomSandbox : MonoBehaviour
    {
        // FurnitureRoot = Transform แม่ที่เก็บเฟอร์นิเจอร์ของแผนที่ปัจจุบัน (StartScreen จะตั้งให้ตามแผนที่ที่เลือก)
        public Transform FurnitureRoot;
        // วัสดุสำหรับเฟอร์นิเจอร์พื้นฐาน: ไม้ (Wood), ผ้า (Fabric), สีเข้ม (Dark), และ Accent (ประกาศไว้ แต่ไม่ได้ใช้ในไฟล์นี้)
        public Material Wood, Fabric, Dark, Accent;
        // StatusLabel = ข้อความ 3 มิติที่แสดงสถานะ/จำนวนชิ้น/ราคารวม
        public TextMesh StatusLabel;
        // Items = รายการเฟอร์นิเจอร์ทั้งหมดในห้อง (readonly = เปลี่ยนตัวลิสต์ไม่ได้ แต่เพิ่ม/ลบสมาชิกได้)
        public readonly List<FurnitureItem> Items = new List<FurnitureItem>();
        // Held = ชิ้นที่ผู้เล่นกำลังถืออยู่ (null = ไม่ได้ถือ) อ่านได้ทุกที่ แต่ตั้งค่าได้เฉพาะในคลาสนี้
        public FurnitureItem Held { get; private set; }
        // Snap = เปิดการจับเข้ากริด 0.25 ม. หรือไม่ (ค่าเริ่มต้น เปิด)
        public bool Snap = true;
        // HalfSize = ครึ่งหนึ่งของขนาดห้อง (ค่าเริ่มต้น 4×3 ม. = ห้อง 8×6 ม.)
        public Vector2 HalfSize = new Vector2(4,3);
        // StartScreen = ตัวจัดการหน้าเมนู/เลือกแผนที่ (ใช้ส่งต่อคำสั่งเมนู)
        public StartScreen StartScreen;
        // LayoutKey = ชื่อ key ของแผนที่ปัจจุบัน (ใช้ตั้งชื่อไฟล์เซฟ)
        public string LayoutKey;
        // Library = คลังเฟอร์นิเจอร์ (ApartmentLibrary)
        public ApartmentLibrary Library;
        // AssetMenu = เมนูคลังเฟอร์นิเจอร์
        public AssetMenu AssetMenu;
        // HoldHeight = ความสูงจากพื้นของชิ้นที่ถืออยู่ (ปรับด้วย Q/E หรือสติ๊กขวา)
        public float HoldHeight { get; private set; }
        // Status = ข้อความสถานะปัจจุบัน (ค่าเริ่มต้น 'Choose a piece to start designing')
        public string Status { get; private set; } = "Choose a piece to start designing";
        // originalPosition = ตำแหน่งเดิมของชิ้นที่ยกขึ้นมา (ไว้คืนเมื่อกดยกเลิก)
        Vector3 originalPosition;
        // originalRotation = การหมุนเดิมของชิ้นนั้น
        Quaternion originalRotation;
        // isNew = ชิ้นที่ถืออยู่เป็นชิ้นใหม่ (true) หรือหยิบของเดิมที่วางไว้แล้ว (false)
        bool isNew;
        // overlaps = บัฟเฟอร์ Collider 128 ช่อง ใช้ตอนตรวจการซ้อนทับ (ไม่สร้างขยะทุกครั้ง)
        readonly Collider[] overlaps = new Collider[128];
        // Limit = จำนวนเฟอร์นิเจอร์สูงสุดต่อห้อง (100 ชิ้น)
        public const int Limit = 100;
        // IsPlaced(item): ชิ้นนั้นถือว่า 'วางแล้ว' = ไม่ null และไม่ใช่ชิ้นใหม่ที่ยังถืออยู่ (ชิ้นใหม่ยังไม่นับราคา)
        public bool IsPlaced(FurnitureItem item) => item && !(isNew && Held==item);
        // TryGetPrice(item,out price): หาราคาของชิ้น คืน true ถ้ามีราคา
        public bool TryGetPrice(FurnitureItem item,out int price)
        {
            // ตั้ง price=0 ไว้ก่อน
            price=0;
            // ถ้าไม่มี AssetKey (เฟอร์นิเจอร์พื้นฐาน) ใช้ราคาจาก FurnitureCatalog.Prices[Id] แล้วคืน true
            if(string.IsNullOrEmpty(item.AssetKey)) { price=FurnitureCatalog.Prices[item.Id];return true; }
            // ไม่งั้นหา entry ในคลังจาก AssetKey (ถ้าไม่มีคลังเป็น null)
            var entry=Library?Library.Find(item.AssetKey):null;
            // ถ้าไม่พบ entry หรือไม่ได้ตั้งราคา หรือราคาติดลบ → คืน false (ไม่มีราคา)
            if(entry==null || !entry.HasPrice || entry.PriceTHB<0) return false;
            // ไม่งั้นเอาราคาจาก entry แล้วคืน true
            price=entry.PriceTHB;return true;
        }
        // Total: ราคารวมของทุกชิ้นที่วางแล้วและมีราคา (ใช้ long กันตัวเลขล้น)
        public long Total { get { long total=0; foreach(var item in Items) if(IsPlaced(item) && TryGetPrice(item,out int price)) total+=price; return total; } }
        // ItemName(item): ชื่อที่แสดง — พื้นฐานใช้ชื่อจาก FurnitureCatalog, ของจากคลังใช้ชื่อใน entry ถ้าไม่พบใช้ 'Apartment asset'
        public string ItemName(FurnitureItem item) => string.IsNullOrEmpty(item.AssetKey)?FurnitureCatalog.Names[item.Id]:Library.Find(item.AssetKey)?.Name??"Apartment asset";
        // AddAsset(key,position,rotation): สร้างชิ้นจากคลังตาม key ที่ตำแหน่ง/การหมุนที่กำหนด
        public FurnitureItem AddAsset(string key,Vector3 position,Quaternion rotation)
        {
            // หา entry จากคลังด้วย key (ถ้าไม่มีคลังให้เป็น null)
            var entry=Library?Library.Find(key):null;
            // ถ้าไม่พบ entry หรือ entry ไม่มี Prefab โยนข้อผิดพลาด 'Missing catalog asset'
            if(entry==null || !entry.Prefab) throw new System.ArgumentException("Missing catalog asset: "+key);
            // สร้างสำเนา Prefab ที่ตำแหน่ง/การหมุน เป็นลูกของ FurnitureRoot
            var go=Instantiate(entry.Prefab,position,rotation,FurnitureRoot);
            // ดึง FurnitureItem จากชิ้นที่สร้าง ใส่ในรายการ Items แล้วคืน
            var item=go.GetComponent<FurnitureItem>(); Items.Add(item); return item;
        }
        // BeginAsset(key): เริ่มถือชิ้นใหม่จากคลัง
        public void BeginAsset(string key)
        {
            // ยกเลิกสิ่งที่ถืออยู่ก่อน; ถ้าเต็มโควตา 100 ชิ้น แจ้ง 'Room limit reached' แล้วออก
            Cancel(); if(Items.Count>=Limit) { Message("Room limit reached"); return; }
            // สร้างชิ้นที่ origin ถือไว้ HoldHeight=0 ตั้ง isNew=true
            Held=AddAsset(key,Vector3.zero,Quaternion.identity); HoldHeight=0; isNew=true;
            // แจ้งข้อความแนะนำ (ย้าย/ปรับความสูงด้วย Q,E หรือสติ๊กขวาขึ้นลง)
            Message("Position "+ItemName(Held)+" / Q,E or right stick up,down: height");
        }
        // Lift(amount): ปรับความสูงชิ้นที่ถืออยู่ — HoldHeight = (เดิม+amount) จำกัด 0..2.5 ม. แล้วอัปเดตตำแหน่ง y ของชิ้น
        public void Lift(float amount) { if(Held) { HoldHeight=Mathf.Clamp(HoldHeight+amount,0,2.5f); var p=Held.transform.position; p.y=HoldHeight; Held.transform.position=p; } }
        // Message(text): ตั้งข้อความสถานะ แล้วรีเฟรชป้าย
        public void Message(string text) { Status=text; Refresh(); }
        // Refresh(): อัปเดตป้ายสถานะ 3 มิติ
        public void Refresh()
        {
            // ถ้ามี StatusLabel ตั้งข้อความ: ชื่อเกม / จำนวนชิ้น + ราคารวม (THB, มีจุลภาค) / สถานะ / กริด (0.25 ม. หรือ free) + ขนาดห้อง
            if(StatusLabel) StatusLabel.text = "YOUR SPACE / YOUR RULES\n" + Items.Count + " pieces   |   THB " + Total.ToString("N0") + "\n" + Status + "\nGrid: " + (Snap?"0.25 m":"free") + "   |   Room: " + HalfSize.x*2 + " x " + HalfSize.y*2 + " m";
        }
        // Add(id,position,rotation): สร้างเฟอร์นิเจอร์พื้นฐานชนิด id
        public FurnitureItem Add(int id,Vector3 position,Quaternion rotation)
        {
            // สร้างด้วย FurnitureCatalog.Create (ใส่วัสดุไม้/ผ้า/เข้ม)
            var go = FurnitureCatalog.Create(id,FurnitureRoot,Wood,Fabric,Dark);
            // ตั้งตำแหน่งและการหมุน
            go.transform.SetPositionAndRotation(position,rotation);
            // ดึง FurnitureItem ใส่ใน Items แล้วคืน
            var item=go.GetComponent<FurnitureItem>(); Items.Add(item); return item;
        }
        // BeginNew(id): เริ่มถือเฟอร์นิเจอร์พื้นฐานชิ้นใหม่
        public void BeginNew(int id)
        {
            // ยกเลิกสิ่งที่ถืออยู่ก่อน
            Cancel();
            // ถ้าเต็ม 100 ชิ้น แจ้งข้อความแล้วออก
            if(Items.Count>=Limit) { Message("Room limit reached (100 pieces)"); return; }
            // สร้างที่ (0,0,0) ถือไว้ HoldHeight=0 ตั้ง isNew=true
            Held=Add(id,new Vector3(0,0,0),Quaternion.identity); HoldHeight=0; isNew=true;
            // แจ้งข้อความแนะนำ
            Message("Position " + FurnitureCatalog.Names[id] + " / trigger to place");
        }
        // Pick(item): หยิบชิ้นที่วางแล้วขึ้นมาย้าย
        public void Pick(FurnitureItem item)
        {
            // ถ้า item เป็น null หรือไม่ได้อยู่ในรายการ ออก
            if(!item || !Items.Contains(item)) return;
            // ยกเลิกสิ่งที่ถืออยู่ก่อน, ตั้ง Held=item, isNew=false, จำตำแหน่ง/การหมุนเดิมไว้ (ไว้คืนถ้ายกเลิก)
            Cancel(); Held=item; isNew=false; originalPosition=item.transform.position; originalRotation=item.transform.rotation;
            // HoldHeight = ความสูงปัจจุบันของชิ้น แล้วแจ้ง 'Moving ...'
            HoldHeight=item.transform.position.y; Message("Moving " + ItemName(item));
        }
        // Move(point): ย้ายชิ้นที่ถืออยู่ไปยังจุดบนพื้นที่เล็ง
        public void Move(Vector3 point)
        {
            // ถ้าไม่ได้ถือ ออก
            if(!Held) return;
            // ตั้งความสูง y = HoldHeight
            point.y=HoldHeight;
            // ถ้าเปิด Snap ปัดพิกัด x,z เป็นทวีคูณของ 0.25 ม. (ปัดเศษ ×4 ÷4)
            if(Snap) { point.x=Mathf.Round(point.x*4)/4; point.z=Mathf.Round(point.z*4)/4; }
            // ตั้งตำแหน่งของชิ้น
            Held.transform.position=point;
        }
        // Rotate(degrees): หมุนชิ้นที่ถืออยู่รอบแกนตั้ง (ในพิกัดโลก) ตามองศาที่ให้
        public void Rotate(float degrees) { if(Held) Held.transform.Rotate(0,degrees,0,Space.World); }
        // IsValid(item): ตรวจว่าตำแหน่งปัจจุบันของชิ้นนี้ 'วางได้' หรือไม่
        public bool IsValid(FurnitureItem item)
        {
            // บังคับให้ฟิสิกส์ซิงก์ตำแหน่ง Transform ล่าสุดก่อนตรวจ
            Physics.SyncTransforms();
            // box = BoxCollider ของชิ้น, bounds = ขอบเขตกล่องในพิกัดโลก
            var box=item.GetComponent<BoxCollider>(); var bounds=box.bounds;
            // ไม่ผ่านถ้าจมพื้น (ต่ำกว่า -0.02) หรือสูงเกิน 3 ม.
            if(bounds.min.y<-.02f || bounds.max.y>3) return false;
            // ไม่ผ่านถ้าล้นขอบห้อง (เผื่อ 5 ซม. จากผนัง) ทั้งแกน x และ z
            if(bounds.min.x< -HalfSize.x+.05f || bounds.max.x>HalfSize.x-.05f || bounds.min.z< -HalfSize.y+.05f || bounds.max.z>HalfSize.y-.05f) return false;
            // ตรวจ Collider ที่ซ้อนทับกล่องของชิ้น (ใช้กล่องเล็กลงเหลือ 49% ไม่ให้ชนผิวสัมผัส) เก็บลง overlaps และได้จำนวน count
            int count=Physics.OverlapBoxNonAlloc(box.transform.TransformPoint(box.center),box.size*.49f,overlaps,item.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            // ถ้าบัฟเฟอร์เต็ม ถือว่าไม่ผ่าน
            if(count==overlaps.Length) return false;
            // ไม่ผ่านถ้าซ้อนกับ Collider อื่น (ไม่ใช่ตัวเอง) ที่เป็นเฟอร์นิเจอร์หรือสิ่งกีดขวาง
            for(int i=0;i<count;i++) if(overlaps[i]!=box && (overlaps[i].GetComponent<FurnitureItem>() || overlaps[i].GetComponent<RoomObstacle>())) return false;
            // ผ่านทุกข้อ คืน true
            return true;
        }
        // Place(): วางชิ้นที่ถืออยู่ลงตำแหน่งปัจจุบัน
        public void Place()
        {
            // ถ้าไม่ได้ถือ ออก
            if(!Held) return;
            // ถ้าวางไม่ได้ (IsValid=false) แจ้ง 'Blocked...' แล้วออก
            if(!IsValid(Held)) { Message("Blocked: keep inside room and clear of furniture"); return; }
            // วางสำเร็จ: ปล่อยมือ (Held=null), isNew=false, แจ้งข้อความ
            Held=null; isNew=false; Message("Placed / point at a piece to move it");
        }
        // Cancel(): ยกเลิกการถือชิ้นปัจจุบัน
        public void Cancel()
        {
            // ถ้าไม่ได้ถือ ออก
            if(!Held) return;
            // ถ้าเป็นชิ้นใหม่: เอาออกจากรายการ ซ่อน และทำลายทิ้ง
            if(isNew) { Items.Remove(Held); Held.gameObject.SetActive(false); Destroy(Held.gameObject); }
            // ถ้าเป็นชิ้นเดิมที่หยิบมา: คืนตำแหน่ง/การหมุนเดิม
            else Held.transform.SetPositionAndRotation(originalPosition,originalRotation);
            // ปล่อยมือ (Held=null, isNew=false) แล้วรีเฟรชป้าย
            Held=null; isNew=false; Refresh();
        }
        // Delete(): ลบชิ้นที่ถืออยู่
        public void Delete()
        {
            // ถ้าไม่ได้ถืออะไร แจ้งให้เลือกก่อนแล้วออก
            if(!Held) { Message("Select a piece before deleting"); return; }
            // เอาออกจากรายการ ซ่อน ทำลาย ปล่อยมือ แล้วแจ้ง 'Piece removed'
            Items.Remove(Held); Held.gameObject.SetActive(false); Destroy(Held.gameObject); Held=null; isNew=false; Message("Piece removed");
        }
        // Clear(): ล้างเฟอร์นิเจอร์ทั้งห้อง
        public void Clear()
        {
            // ปล่อยมือ
            Held=null; isNew=false;
            // ซ่อนและทำลายทุกชิ้น
            foreach(var item in Items) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            // ล้างรายการ แล้วแจ้ง 'Room cleared'
            Items.Clear(); Message("Room cleared");
        }
        // Command(button): รับคำสั่งจากปุ่ม 3 มิติ ตามรหัส button.Command
        public void Command(RoomButton button)
        {
            // แยกตามรหัสคำสั่ง
            switch(button.Command)
            {
                // 0 = เริ่มถือเฟอร์นิเจอร์พื้นฐานชิ้นที่ ItemId
                case 0: BeginNew(button.ItemId); break;
                // 1 = บันทึกเลย์เอาต์ (LayoutStore.Save)
                case 1: GetComponent<LayoutStore>().Save(); break;
                // 2 = โหลดเลย์เอาต์ (LayoutStore.Load)
                case 2: GetComponent<LayoutStore>().Load(); break;
                // 3 = สลับเปิด/ปิดการจับกริด
                case 3: Snap=!Snap; Refresh(); break;
                // 4 = ลบชิ้นที่ถือ
                case 4: Delete(); break;
                // 5 = ยกเลิกการย้าย
                case 5: Cancel(); Message("Move cancelled"); break;
                // 6 = หมุน 45°
                case 6: Rotate(45); break;
                // 7 = กลับหน้าเมนูหลัก (StartScreen.Show)
                case 7: StartScreen.Show(); break;
                // 10 = เข้าแผนที่ (StartScreen.Enter)
                case 10: StartScreen.Enter(); break;
                // 11 = เลือกแผนที่ลำดับ ItemId (StartScreen.SelectMap)
                case 11: StartScreen.SelectMap(button.ItemId); break;
                // 12 = สลับเมนูคลังเฟอร์นิเจอร์ (AssetMenu.Toggle)
                case 12: AssetMenu.Toggle(); break;
                // 13 = เปลี่ยนหน้าเมนูคลัง (ItemId = -1 หรือ +1)
                case 13: AssetMenu.ChangePage(button.ItemId); break;
                // 14 = สร้างชิ้นจากคลังลำดับ ItemId (AssetMenu.Spawn)
                case 14: AssetMenu.Spawn(button.ItemId); break;
            }
        }
        // OnEnable(): เมื่อคอมโพเนนต์เปิดใช้งาน ถ้ามี FurnitureRoot ให้ล้าง Items แล้วดึงเฟอร์นิเจอร์ทั้งหมดใต้ราก (รวมที่ปิดอยู่) ใส่ใหม่ และรีเฟรชป้าย
        void OnEnable() { if(!FurnitureRoot) return; Items.Clear(); Items.AddRange(FurnitureRoot.GetComponentsInChildren<FurnitureItem>(true)); Refresh(); }
    }
}
