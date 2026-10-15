// ==== FurniturePreview.cs ====
// สร้างรูปตัวอย่าง (thumbnail) ของ prefab เฟอร์นิเจอร์ด้วยการ 'ถ่ายภาพ' ด้วยกล้องบนเวทีลับ แล้วเก็บเป็น RenderTexture (แคชไว้)
// ใช้โดย AssetMenu.OnGUI เพื่อโชว์รูปบนการ์ดแต่ละใบ
// นำเข้า System.Collections.Generic — ใช้ Dictionary
using System.Collections.Generic;
// นำเข้า UnityEngine — ใช้ Camera, Light, RenderTexture ฯลฯ
using UnityEngine;

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // หมายเหตุเดิม: เรนเดอร์เมื่อต้องการเท่านั้น วัตถุตัวอย่างจะถูกปิดไว้นอกช่วงที่กำลังเรนเดอร์
    // Render only on demand. Preview objects are inactive outside the render call.
    // คลาสธรรมดา (ไม่ใช่ MonoBehaviour) จัดการเวทีถ่ายภาพ + แคชรูป
    public sealed class FurniturePreview
    {
        // images: แคชรูป — key = Key ของ asset, value = RenderTexture ที่เรนเดอร์แล้ว
        readonly Dictionary<string,RenderTexture> images=new Dictionary<string,RenderTexture>();
        // stage: GameObject เวทีถ่ายภาพ (สร้างครั้งแรกที่ต้องใช้)
        GameObject stage;
        // camera: กล้องที่ใช้ถ่ายตัวอย่าง
        Camera camera;
        // Get(entry): คืนรูปตัวอย่างของ entry (ถ้ายังไม่มีก็เรนเดอร์ให้)
        public Texture Get(ApartmentLibrary.Entry entry)
        {
            // ถ้ามีรูปในแคชแล้ว คืนทันที (out var image คือค่าที่ดึงได้)
            if(images.TryGetValue(entry.Key,out var image)) return image;
            // ถ้า entry ไม่มี Prefab ให้คืน null (ไม่มีอะไรให้ถ่าย)
            if(!entry.Prefab) return null;
            // ถ้ายังไม่มีเวที ให้สร้างครั้งแรก:
            if(!stage)
            {
                // สร้าง GameObject เวที แล้วปิด (SetActive false) ไว้ก่อน
                stage=new GameObject("Furniture preview stage");stage.SetActive(false);
                // ย้ายเวทีไปที่ y = -1000 ให้ไกลจากฉากจริง
                stage.transform.position=new Vector3(0,-1000,0);
                // สร้างวัตถุ 'Preview camera' เป็นลูกของเวที
                var view=new GameObject("Preview camera");view.transform.SetParent(stage.transform,false);
                // เพิ่มคอมโพเนนต์ Camera ปิดการทำงานอัตโนมัติ (จะสั่ง Render เอง) และให้เห็นเฉพาะเลเยอร์ 31
                camera=view.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;
                // ล้างพื้นหลังเป็นสีเดียว (เขียวเข้มเทา)
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.19f,.20f);
                // ปิดโหมดตาคู่ (VR), ตั้ง FOV 32°, สัดส่วนภาพ 1.35
                camera.stereoTargetEye=StereoTargetEyeMask.None;camera.fieldOfView=32;camera.aspect=1.35f;
                // สร้างวัตถุไฟ 'Preview light' เป็นลูกของเวที
                var lightObject=new GameObject("Preview light");lightObject.transform.SetParent(stage.transform,false);
                // เพิ่ม Light แบบ Directional ความสว่าง 1.4 และให้ส่องเฉพาะเลเยอร์ 31
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;
                // หมุนไฟเอียง 45° ลง และ -30° ข้าง เพื่อให้เกิดเงาดูมีมิติ
                lightObject.transform.localRotation=Quaternion.Euler(45,-30,0);
            }
            // สร้างสำเนาของ prefab เป็นลูกของเวที (worldPositionStays = false)
            var model=Object.Instantiate(entry.Prefab,stage.transform,false);
            // ตั้งทุก Transform ในโมเดล (รวมที่ปิดอยู่) ให้อยู่เลเยอร์ 31 เพื่อให้กล้องตัวอย่างเห็น
            foreach(var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=31;
            // ปิด Collider ทั้งหมดของโมเดลตัวอย่าง ไม่ให้ไปรบกวนฟิสิกส์ในฉาก
            foreach(var c in model.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            // ปิดสคริปต์ MonoBehaviour ทุกตัวในโมเดลตัวอย่าง ไม่ให้โค้ดเกมทำงานกับของปลอม
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled=false;
            // target = RenderTexture ที่จะสร้าง (null ไว้ก่อน เผื่อต้องเก็บกวาดเมื่อเกิดข้อผิดพลาด)
            RenderTexture target=null;
            // เริ่ม try เพื่อให้ finally ด้านล่างคืนสภาพทุกครั้ง
            try
            {
                // เปิดเวทีให้ทำงาน (กล้อง/ไฟ/โมเดลเห็นได้)
                stage.SetActive(true);
                // รวบรวม Renderer ที่เปิดอยู่ของโมเดล
                var renderers=model.GetComponentsInChildren<Renderer>();
                // ถ้าไม่มี Renderer เลยให้คืน null (finally ยังทำงานเก็บกวาด)
                if(renderers.Length==0) return null;
                // รวมขอบเขต (Bounds) ของทุก Renderer เป็นกล่องเดียวที่ครอบทั้งโมเดล
                Bounds bounds=renderers[0].bounds;foreach(var r in renderers) bounds.Encapsulate(r.bounds);
                // radius = รัศมีโดยประมาณของโมเดล (ครึ่งเส้นทแยงมุมกล่อง) ขั้นต่ำ 0.05
                float radius=Mathf.Max(.05f,bounds.extents.magnitude);
                // คำนวณระยะกล้องให้ทรงกลมรัศมี radius พอดีในมุมมอง: radius / sin(FOV/2) แล้วเผื่อเพิ่ม 12%
                float distance=radius/Mathf.Sin(camera.fieldOfView*Mathf.Deg2Rad*.5f)*1.12f;
                // วางกล้องห่างจากศูนย์กลางโมเดลที่ระยะ distance ในทิศ (1, 0.7, -1) = มองเฉียงจากมุมบนขวาด้านหน้า
                camera.transform.position=bounds.center+new Vector3(1,.7f,-1).normalized*distance;
                // หันกล้องไปที่ศูนย์กลางโมเดล, ตั้ง near clip 0.01 และ far clip ให้เห็นโมเดลทั้งก้อน
                camera.transform.LookAt(bounds.center);camera.nearClipPlane=.01f;camera.farClipPlane=distance+radius*3;
                // สร้าง RenderTexture ขนาด 324×240 depth 16 บิต ตั้งชื่อ แล้ว Create() ให้จองหน่วยความจำจริง
                target=new RenderTexture(324,240,16){name="Preview / "+entry.Name};target.Create();
                // ผูกกล้องกับ texture, สั่ง Render 1 ครั้ง, เก็บเข้าแคช แล้วคืน texture
                camera.targetTexture=target;camera.Render();images.Add(entry.Key,target);return target;
            }
            // catch: ถ้าเกิดข้อผิดพลาด ให้ปล่อยและทำลาย RenderTexture ที่สร้างค้างไว้ แล้ว throw ต่อให้ผู้เรียกรับรู้
            catch { if(target) { target.Release();Object.Destroy(target); } throw; }
            // finally (ทำเสมอ): ถอดกล้องออกจาก texture, ปิดเวที, ซ่อนและทำลายโมเดลตัวอย่าง
            finally { camera.targetTexture=null;stage.SetActive(false);model.SetActive(false);Object.Destroy(model); }
        }
        // Clear(): ล้างแคชรูปทั้งหมด
        public void Clear()
        {
            // ปล่อยหน่วยความจำ GPU ของทุก RenderTexture และทำลาย object แล้วเคลียร์ dictionary
            foreach(var image in images.Values) { image.Release();Object.Destroy(image); }images.Clear();
        }
        // Dispose(): ล้างแคชและทำลายเวทีทั้งอัน (เรียกตอน AssetMenu ถูกทำลาย)
        public void Dispose() { Clear();if(stage) Object.Destroy(stage); }
    }
}
