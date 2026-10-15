// ==== LayoutStore.cs ====
// บันทึก (Save) / โหลด (Load) เลย์เอาต์เฟอร์นิเจอร์ของแต่ละแผนที่ เป็นไฟล์ JSON ในโฟลเดอร์ข้อมูลของแอป
// มีการตรวจความถูกต้อง, เขียนผ่านไฟล์ชั่วคราว, สำรอง .bak, และโหลดแบบ 'ทดลองก่อน' เพื่อไม่ให้ห้องปัจจุบันเสียหายถ้าไฟล์เซฟมีปัญหา
// นำเข้า System — ใช้ [Serializable], Exception
using System;
// นำเข้า System.IO — ใช้ File, Path, FileInfo, InvalidDataException
using System.IO;
// นำเข้า System.Collections.Generic
using System.Collections.Generic;
// นำเข้า UnityEngine
using UnityEngine;

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // บังคับให้ GameObject ที่ติดคอมโพเนนต์นี้ต้องมี RoomSandbox ด้วย (Unity จะเพิ่มให้/กันไม่ให้ลบ)
    [RequireComponent(typeof(RoomSandbox))]
    // คลาส LayoutStore (MonoBehaviour)
    public sealed class LayoutStore : MonoBehaviour
    {
        // Entry = ข้อมูลเฟอร์นิเจอร์ 1 ชิ้นที่บันทึก: id, assetKey, position (ตำแหน่ง), yaw (มุมหมุนรอบแกนตั้ง องศา)
        [Serializable] public sealed class Entry { public int id; public string assetKey; public Vector3 position; public float yaw; }
        // Layout = ไฟล์เซฟทั้งก้อน: version (เวอร์ชันรูปแบบ = 1) และรายการ items
        [Serializable] public sealed class Layout { public int version=1; public List<Entry> items=new List<Entry>(); }
        // SavePath: พาธไฟล์เซฟของแผนที่ปัจจุบัน (property แบบอ่านอย่างเดียว คำนวณใหม่ทุกครั้งที่อ่าน)
        public string SavePath
        {
            // getter: ส่วนที่คำนวณและคืนค่าพาธ
            get
            {
                // room = RoomSandbox บน GameObject เดียวกัน
                var room=GetComponent<RoomSandbox>();
                // key = LayoutKey ของแผนที่ ถ้าว่างให้เดาจากขนาดห้อง: กว้างครึ่งหนึ่ง >4 ม. = 'modern-condo' ไม่งั้น 'home-sandbox'
                // (แต่ละแผนที่จึงเซฟแยกไฟล์กัน)
                string key=string.IsNullOrEmpty(room.LayoutKey)?(room.HalfSize.x>4?"modern-condo":"home-sandbox"):room.LayoutKey;
                // รวมโฟลเดอร์ข้อมูลถาวรของแอป (persistentDataPath) กับชื่อ key + '-v1.json'
                return Path.Combine(Application.persistentDataPath,key+"-v1.json");
            }
        }
        // Validate(layout): ตรวจว่าข้อมูลเลย์เอาต์ปลอดภัยพอจะโหลดหรือไม่
        public static bool Validate(Layout layout)
        {
            // ไม่ผ่านถ้า layout เป็น null, version ไม่ใช่ 1, รายการเป็น null หรือเกินจำนวนสูงสุด (Limit = 100 ชิ้น)
            if(layout==null || layout.version!=1 || layout.items==null || layout.items.Count>RoomSandbox.Limit) return false;
            // ตรวจแต่ละรายการ
            foreach(var e in layout.items)
                // ไม่ผ่านถ้า: รายการเป็น null / (ไม่มี assetKey และ id อยู่นอกช่วงเฟอร์นิเจอร์พื้นฐาน) / ตัวเลขตำแหน่งหรือมุมเป็น NaN/อนันต์ /
                // |x| หรือ |z| เกิน 100 เมตร / y ต่ำกว่า 0 หรือสูงกว่า 2.5 เมตร
                if(e==null || (string.IsNullOrEmpty(e.assetKey) && (e.id<0 || e.id>=FurnitureCatalog.Names.Length)) || !Finite(e.position.x) || !Finite(e.position.y) || !Finite(e.position.z) || !Finite(e.yaw) || Mathf.Abs(e.position.x)>100 || Mathf.Abs(e.position.z)>100 || e.position.y<0 || e.position.y>2.5f) return false;
            // ผ่านทุกข้อ คืน true
            return true;
        }
        // Finite(value): เป็นตัวเลขจริง (ไม่ใช่ NaN และไม่ใช่อนันต์) หรือไม่
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        // Save(): บันทึกเลย์เอาต์ปัจจุบัน
        public void Save()
        {
            // room = RoomSandbox
            var room=GetComponent<RoomSandbox>();
            // ถ้ากำลังถือเฟอร์นิเจอร์อยู่ ห้ามบันทึก (แจ้งข้อความแล้วออก) เพราะชิ้นนั้นยังไม่ได้วางจริง
            if(room.Held) { room.Message("Place or cancel the current piece before saving"); return; }
            // try เพื่อดักข้อผิดพลาดเรื่องไฟล์/ดิสก์
            try
            {
                // สร้าง Layout และเพิ่ม Entry ของทุกชิ้นใน room.Items (id, assetKey, ตำแหน่งโลก, มุม yaw)
                var data=new Layout(); foreach(var item in room.Items) data.items.Add(new Entry{id=item.Id,assetKey=item.AssetKey,position=item.transform.position,yaw=item.transform.eulerAngles.y});
                // เขียน JSON (จัดรูปแบบอ่านง่าย) ลงไฟล์ชั่วคราว .tmp ก่อน
                string temp=SavePath+".tmp"; File.WriteAllText(temp,JsonUtility.ToJson(data,true));
                // ถ้ามีไฟล์เซฟเดิม คัดลอกไปเป็น .bak (สำรองเวอร์ชันก่อนหน้า, ทับของเก่าได้)
                if(File.Exists(SavePath)) File.Copy(SavePath,SavePath+".bak",true);
                // คัดลอกไฟล์ชั่วคราวเป็นไฟล์จริง ลบไฟล์ชั่วคราว แล้วแจ้งว่าบันทึกสำเร็จ
                File.Copy(temp,SavePath,true); File.Delete(temp); room.Message("Layout saved on this device");
            }
            // catch: เก็บ log เตือน และแจ้งผู้เล่นว่าบันทึกไม่ได้ (ของสำรองยังอยู่)
            catch(Exception e) { Debug.LogWarning("Save failed: "+e.Message); room.Message("Could not save layout; previous backup retained"); }
        }
        // Load(): โหลดเลย์เอาต์ที่บันทึกไว้
        public void Load()
        {
            // room = RoomSandbox
            var room=GetComponent<RoomSandbox>();
            // ถ้ากำลังถือเฟอร์นิเจอร์อยู่ ห้ามโหลด
            if(room.Held) { room.Message("Place or cancel the current piece before loading"); return; }
            // try เพื่อดักข้อผิดพลาดทั้งหมด
            try
            {
                // ถ้ายังไม่มีไฟล์เซฟ แจ้งข้อความแล้วออก
                if(!File.Exists(SavePath)) { room.Message("No saved layout on this device yet"); return; }
                // ถ้าไฟล์ใหญ่เกิน 1,000,000 ไบต์ (~1 MB) โยนข้อผิดพลาด (ป้องกันไฟล์ผิดปกติ)
                if(new FileInfo(SavePath).Length>1000000) throw new InvalidDataException("Save file too large");
                // อ่านไฟล์แล้วแปลง JSON เป็นออบเจกต์ Layout
                var data=JsonUtility.FromJson<Layout>(File.ReadAllText(SavePath));
                // ตรวจ Validate ไม่ผ่านก็โยนข้อผิดพลาด
                if(!Validate(data)) throw new InvalidDataException("Invalid or unsupported layout");
                // ตรวจว่าทุก assetKey ที่ใช้ ยังมีอยู่ในคลัง (Library) ไม่งั้นโยนข้อผิดพลาด 'Missing asset'
                foreach(var entry in data.items) if(!string.IsNullOrEmpty(entry.assetKey) && (!room.Library || room.Library.Find(entry.assetKey)==null)) throw new InvalidDataException("Missing asset: "+entry.assetKey);
                // หมายเหตุเดิม: สร้างและตรวจเรขาคณิตแบบ 'staged' ก่อนจะแทนที่ห้องปัจจุบันของผู้ใช้
                // Stage and validate geometry before replacing the user's current room.
                // old = สำเนารายการเฟอร์นิเจอร์ที่มีอยู่ตอนนี้
                var old=new List<FurnitureItem>(room.Items);
                // ซ่อนของเก่าทั้งหมดชั่วคราว (ยังไม่ทำลาย) เพื่อให้ตรวจการชนของชิ้นใหม่ได้ถูกต้อง
                foreach(var item in old) item.gameObject.SetActive(false);
                // staged = รายการชิ้นใหม่ที่สร้างระหว่างทดลอง; valid = ผลการตรวจ
                var staged=new List<FurnitureItem>(); bool valid=true;
                // try ภายใน เพื่อให้ถ้าสร้างล้มเหลวกลางทางจะย้อนกลับได้
                try
                {
                    // วนทุก entry ที่บันทึกไว้
                    foreach(var entry in data.items)
                    {
                        // สร้างชิ้นใหม่ — ถ้าไม่มี assetKey ใช้ room.Add (พื้นฐาน) ไม่งั้นใช้ room.AddAsset (จากคลัง) ที่ตำแหน่ง/มุมที่บันทึก แล้วเก็บเข้า staged
                        var item=string.IsNullOrEmpty(entry.assetKey)?room.Add(entry.id,entry.position,Quaternion.Euler(0,entry.yaw,0)):room.AddAsset(entry.assetKey,entry.position,Quaternion.Euler(0,entry.yaw,0)); staged.Add(item);
                        // ตรวจว่าชิ้นนี้วางได้ถูกต้อง (อยู่ในห้อง ไม่ชนกัน) ถ้าไม่ได้ → valid=false และหยุด
                        if(!room.IsValid(item)) { valid=false; break; }
                    }
                }
                // ถ้าสร้างล้มเหลวด้วยข้อยกเว้น ถือว่าไม่ valid
                catch { valid=false; }
                // ถ้าไม่ valid ให้ย้อนกลับ:
                if(!valid)
                {
                    // ทำลายชิ้นใหม่ที่สร้างไปแล้วทั้งหมด (เอาออกจาก Items, ซ่อน, ทำลาย)
                    foreach(var item in staged) { room.Items.Remove(item); item.gameObject.SetActive(false); Destroy(item.gameObject); }
                    // คืนของเก่ากลับมาแสดง
                    foreach(var item in old) item.gameObject.SetActive(true);
                    // โยนข้อผิดพลาด 'Layout contains blocked placements'
                    throw new InvalidDataException("Layout contains blocked placements");
                }
                // ถ้า valid ทั้งหมด: เอาของเก่าออกจากรายการและทำลายทิ้ง
                foreach(var item in old) { room.Items.Remove(item); Destroy(item.gameObject); }
                // แจ้งผู้เล่นว่ากู้เลย์เอาต์สำเร็จ
                room.Message("Saved layout restored");
            }
            // catch ใหญ่: เก็บ log และแจ้งว่าโหลดไม่สำเร็จ ห้องปัจจุบันยังอยู่ครบ
            catch(Exception e) { Debug.LogWarning("Load failed: "+e.Message); room.Message("Load failed; current layout preserved"); }
        }
    }
}
