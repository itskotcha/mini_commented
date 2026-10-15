// ==== SandboxInput.cs ====
// ตัวควบคุมผู้เล่นทั้งหมด: บนคอมพิวเตอร์ (WASD เดิน, คลิกขวาหมุนมุมมอง, คลิกซ้ายจัดเรียง, R/Q/E/Del/Esc/Tab/F5/F9)
// และใน VR (Quest: ตามศีรษะ/มือ, เลเซอร์มือขวา, เทเลพอร์ตมือซ้าย) พร้อมเมนูห้องและตารางสรุปราคาแบบ IMGUI
// นำเข้า System.Collections.Generic — ใช้ List, Dictionary
using System.Collections.Generic;
// นำเข้า UnityEngine
using UnityEngine;
// นำเข้า UnityEngine.XR — ใช้ XRSettings, InputDevices, XRNode
using UnityEngine.XR;
// กำหนดนามแฝง CommonUsages ให้ชี้ไปยัง UnityEngine.XR.CommonUsages (กันความกำกวมกับ CommonUsages ของ Input System)
using CommonUsages = UnityEngine.XR.CommonUsages;
// ถ้าโปรเจกต์เปิดใช้ Input System ใหม่ (สัญลักษณ์ ENABLE_INPUT_SYSTEM)
#if ENABLE_INPUT_SYSTEM
// นำเข้า UnityEngine.InputSystem — ใช้ Mouse, Keyboard
using UnityEngine.InputSystem;
// จบเงื่อนไข #if
#endif

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส SandboxInput (MonoBehaviour) ติดบน 'rig' ของผู้เล่น (กล้องเป็นลูก)
    public sealed class SandboxInput : MonoBehaviour
    {
        // Room = ตัวจัดการห้อง (RoomSandbox)
        public RoomSandbox Room;
        // View = กล้องของผู้เล่น (ตามศีรษะใน VR)
        public Camera View;
        // Pointer = เส้นเลเซอร์จากมือขวา (VR)
        public LineRenderer Pointer;
        // TeleportPointer = เส้นเลเซอร์เทเลพอร์ตจากมือซ้าย (VR)
        public LineRenderer TeleportPointer;
        // InMenu = ตอนนี้อยู่ในหน้าเมนูหลักหรือไม่ (StartScreen เปิดอยู่)
        bool InMenu => Room.StartScreen && Room.StartScreen.IsOpen;
        // subsystems = รายการระบบ XR (ใช้ตั้งโหมดจุดอ้างอิงการติดตาม)
        readonly List<XRInputSubsystem> subsystems=new List<XRInputSubsystem>();
        // hits = บัฟเฟอร์ผล Raycast 128 ช่อง (ไม่สร้างขยะทุกเฟรม)
        readonly RaycastHit[] hits=new RaycastHit[128];
        // ตัวแปรจำสถานะปุ่มเฟรมก่อนหน้า (ไว้ตรวจ 'เพิ่งกด'): trigger ขวา, trigger ซ้าย, ปุ่ม A, ปุ่ม B; stickLatch = ล็อกการหมุนด้วยสติ๊กให้หมุนทีละครั้ง
        bool previousTrigger,previousLeft,previousA,previousB,stickLatch;
        // previousMenu = สถานะปุ่มเมนู (มือซ้าย) เฟรมก่อน
        bool previousMenu;
        // xrWasActive = เฟรมก่อนหน้าใช้ VR อยู่หรือไม่ (ไว้ตรวจว่า 'เพิ่งเริ่ม VR')
        bool xrWasActive;
        // pitch = มุมก้ม/เงยของกล้องบนจอคอม (องศา)
        float pitch;
        // IsRoomMenuOpen = เมนูห้อง (IMGUI) เปิดอยู่หรือไม่
        public bool IsRoomMenuOpen { get; private set; }
        // blockWorldInputThrough = เลขเฟรมสุดท้ายที่จะบล็อกการคลิกในโลก (กันคลิกทะลุจากเมนูลงโลกหลังเมนู); -1 = ไม่บล็อก
        int blockWorldInputThrough=-1;
        // showPlacedAssets = แท็บในเมนูห้อง: true = ตาราง 'PLACED ASSETS', false = 'ADD BASIC FURNITURE'
        bool showPlacedAssets=true;
        // inventoryScroll = ตำแหน่งเลื่อนของตารางสรุป
        Vector2 inventoryScroll;
        // CloseRoomMenu(): ปิดเมนูห้อง และบล็อกอินพุตโลก 1 เฟรมถัดไป
        public void CloseRoomMenu() { IsRoomMenuOpen=false; blockWorldInputThrough=Time.frameCount+1; }
        // ToggleRoomMenu(): สลับเปิด/ปิดเมนูห้อง
        public void ToggleRoomMenu()
        {
            // ถ้าอยู่หน้าเมนูหลัก ไม่ทำอะไร
            if(InMenu) return;
            // ถ้าเปิดอยู่ → ปิด
            if(IsRoomMenuOpen) CloseRoomMenu();
            // ไม่งั้น: ปิดเมนูคลังเฟอร์นิเจอร์ (ถ้ามี) แล้วเปิดเมนูห้อง
            else { if(Room.AssetMenu) Room.AssetMenu.Close(); IsRoomMenuOpen=true; }
        }
        // ResetDesktopView(): รีเซ็ตมุมก้ม/เงยเป็น 0 และการหมุนโลคอลของกล้อง
        public void ResetDesktopView() { pitch=0; View.transform.localRotation=Quaternion.identity; }
        // Occluded(ray,distance): ตรวจว่ามี 'สิ่งกีดขวาง' (RoomObstacle) บังเส้นทางของ ray ก่อนถึงระยะ distance หรือไม่
        bool Occluded(Ray ray,float distance)
        {
            // ยิง Raycast ไม่จองหน่วยความจำ ระยะ distance-0.02 ทุกเลเยอร์ ไม่นับ trigger เก็บลง hits
            int count=Physics.RaycastNonAlloc(ray,hits,Mathf.Max(0,distance-.02f),~0,QueryTriggerInteraction.Ignore);
            // ถ้าเจอ collider ที่มี RoomObstacle → true (ถูกบัง)
            for(int i=0;i<count;i++) if(hits[i].collider.GetComponent<RoomObstacle>()) return true;
            // ไม่เจอ → false
            return false;
        }
        // Start(): ตั้งเฟรมเรตเป้าหมาย 72 FPS (เหมาะกับ Quest)
        void Start() { Application.targetFrameRate=72; }
        // Update(): ทำงานทุกเฟรม
        void Update()
        {
            // xr = กำลังใช้อุปกรณ์ VR อยู่หรือไม่
            bool xr=XRSettings.isDeviceActive;
            // หมายเหตุเดิม: ปุ่มควบคุมบนคอมอยู่ใน overlay; แผงในโลกเก็บไว้สำหรับใช้กับแว่น
            // Desktop controls live in the overlay; retain the world panel for headset use.
            // ถ้ามี StartScreen และแคตตาล็อกร่วม
            if(Room.StartScreen && Room.StartScreen.SharedCatalog)
            {
                // catalog = GameObject ของแคตตาล็อกร่วม
                var catalog=Room.StartScreen.SharedCatalog.gameObject;
                // แสดงแคตตาล็อกเฉพาะตอนใช้ VR (เปิดเมื่อ xr=true ปิดเมื่อ false)
                if(catalog.activeSelf!=xr) catalog.SetActive(xr);
            }
            // ถ้าอยู่หน้าเมนูหลัก หรือใช้ VR ให้ปิดเมนูห้อง IMGUI
            if(InMenu || xr) IsRoomMenuOpen=false;
            // ถ้า VR เพิ่งเริ่มทำงานในเฟรมนี้
            if(xr && !xrWasActive)
            {
                // ดึงรายการ XRInputSubsystem ทั้งหมด
                SubsystemManager.GetSubsystems(subsystems);
                // ตั้งจุดอ้างอิงการติดตามเป็นระดับพื้น (Floor) ให้ความสูงตัวผู้เล่นตรงกับของจริง
                foreach(var subsystem in subsystems) subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }
            // จำสถานะ VR ของเฟรมนี้ไว้ใช้เฟรมหน้า
            xrWasActive=xr;
            // เลือกอัปเดต: VR → UpdateXR(), ไม่ใช่ → UpdateDesktop()
            if(xr) UpdateXR(); else UpdateDesktop();
        }
        // Pose(device,out position,out rotation): อ่านตำแหน่ง/การหมุนของอุปกรณ์ XR; คืน true เมื่ออ่านได้
        bool Pose(UnityEngine.XR.InputDevice device,out Vector3 position,out Quaternion rotation)
        {
            // ตั้งค่าเริ่มต้นให้ out (ตำแหน่ง 0, ไม่หมุน)
            position=default; rotation=Quaternion.identity;
            // สำเร็จเมื่อ: อุปกรณ์ valid และกำลังถูกติดตาม (isTracked) และอ่านตำแหน่งกับการหมุนได้
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked) && tracked && device.TryGetFeatureValue(CommonUsages.devicePosition,out position) && device.TryGetFeatureValue(CommonUsages.deviceRotation,out rotation);
        }
        // UpdateXR(): ควบคุมเมื่อใช้ VR
        void UpdateXR()
        {
            // หาอุปกรณ์ศีรษะ (headset)
            var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);
            // ถ้าอ่านท่าทางศีรษะได้ ตั้งตำแหน่ง/การหมุนโลคอลของกล้องตามนั้น
            if(Pose(head,out var hp,out var hr)) View.transform.SetLocalPositionAndRotation(hp,hr);
            // หาคอนโทรลเลอร์มือขวา
            var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            // tracked = อ่านท่าทางมือขวาได้หรือไม่ (ได้ตำแหน่ง p, การหมุน q)
            bool tracked=Pose(right,out var p,out var q);
            // เปิดเส้นเลเซอร์มือขวาเมื่อ tracked
            Pointer.enabled=tracked;
            // ถ้ามือขวาถูกติดตาม:
            if(tracked)
            {
                // ray = รังสีจากมือขวา (แปลงพิกัดโลคอลของ rig เป็นพิกัดโลก) ชี้ไปข้างหน้าของมือ
                var ray=new Ray(transform.TransformPoint(p),transform.rotation*q*Vector3.forward);
                // อ่านปุ่ม trigger
                right.TryGetFeatureValue(CommonUsages.triggerButton,out bool trigger);
                // อ่านปุ่ม A (primaryButton)
                right.TryGetFeatureValue(CommonUsages.primaryButton,out bool a);
                // อ่านปุ่ม B (secondaryButton)
                right.TryGetFeatureValue(CommonUsages.secondaryButton,out bool b);
                // อ่านสติ๊กอนาล็อก (primary2DAxis)
                right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 stick);
                // ถ้าไม่ได้อยู่ในเมนูหลัก, สติ๊กแนวนอนเอียงเกิน 0.7 และยังไม่ล็อก:
                if(!InMenu && Mathf.Abs(stick.x)>.7f && !stickLatch)
                {
                    // angle = ±45° ตามทิศสติ๊ก
                    float angle=Mathf.Sign(stick.x)*45;
                    // ถ้ากำลังถือเฟอร์นิเจอร์ หมุนชิ้นนั้น ไม่งั้นหมุนตัวผู้เล่นรอบตำแหน่งกล้อง
                    if(Room.Held) Room.Rotate(angle); else transform.RotateAround(View.transform.position,Vector3.up,angle);
                    // ล็อกไว้ (หมุนครั้งเดียวต่อการเอียงสติ๊ก)
                    stickLatch=true;
                }
                // ปลดล็อกเมื่อสติ๊กกลับมาใกล้กลาง (<0.25)
                if(Mathf.Abs(stick.x)<.25f) stickLatch=false;
                // ปุ่ม A (เพิ่งกด) = ลบชิ้นที่ถือ (ไม่ทำงานในเมนูหลัก)
                if(!InMenu && a && !previousA) Room.Delete();
                // ปุ่ม B (เพิ่งกด) = ยกเลิกการถือ
                if(!InMenu && b && !previousB) Room.Cancel();
                // เล็งและทำงานตามรังสี: ส่งว่า trigger เพิ่งถูกกดหรือไม่ และเส้นเลเซอร์ที่ใช้วาด
                Point(ray,trigger && !previousTrigger,Pointer);
                // จำสถานะปุ่มไว้ใช้เฟรมหน้า
                previousTrigger=trigger; previousA=a; previousB=b;
            }
            // ถ้าไม่ได้ติดตามมือขวา: รีเซ็ตสถานะปุ่ม และถ้ากำลังถืออยู่ให้ยกเลิก
            else { previousTrigger=false; previousA=false; previousB=false; if(Room.Held) Room.Cancel(); }
            // หาคอนโทรลเลอร์มือซ้าย
            var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            // อ่านปุ่มหลักของมือซ้าย (ใช้เป็นปุ่มเมนู)
            left.TryGetFeatureValue(CommonUsages.primaryButton,out bool menu);
            // ถ้าเพิ่งกดปุ่มเมนูและมี AssetMenu → สลับเมนูคลัง; แล้วจำสถานะปุ่ม (บรรทัดหลังทำงานเสมอ ไม่ได้อยู่ในเงื่อนไข if)
            if(menu && !previousMenu && Room.AssetMenu) Room.AssetMenu.Toggle(); previousMenu=menu;
            // ถ้าถือชิ้นอยู่ (และไม่ใช่เมนูหลัก) อ่านสติ๊กขวาแกนตั้ง ถ้าเอียงเกิน 0.5 ยกชิ้นขึ้น/ลง (คูณเวลา)
            if(Room.Held && !InMenu) { right.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 liftStick); if(Mathf.Abs(liftStick.y)>.5f) Room.Lift(liftStick.y*Time.deltaTime); }
            // leftTracked = อ่านท่าทางมือซ้ายได้และไม่อยู่ในเมนูหลัก; เปิดเส้นเลเซอร์เทเลพอร์ตตามนั้น
            bool leftTracked=Pose(left,out p,out q) && !InMenu; TeleportPointer.enabled=leftTracked;
            // ถ้ามือซ้ายถูกติดตาม:
            if(leftTracked)
            {
                // ray = รังสีจากมือซ้าย
                var ray=new Ray(transform.TransformPoint(p),transform.rotation*q*Vector3.forward);
                // อ่านปุ่ม trigger มือซ้าย
                left.TryGetFeatureValue(CommonUsages.triggerButton,out bool trigger);
                // plane = ระนาบพื้น (y=0)
                var plane=new Plane(Vector3.up,Vector3.zero);
                // target = รังสีชนพื้นภายในระยะ 12 ม. หรือไม่
                bool target=plane.Raycast(ray,out float distance) && distance<12;
                // ถ้ามีสิ่งกีดขวางบังก่อนถึงพื้น ให้ถือว่าไม่ได้จุดหมาย
                if(target && Occluded(ray,distance)) target=false;
                // dest = จุดหมาย: ถ้ามี target ใช้จุดที่ชนพื้น ไม่งั้นใช้จุดที่ 6 ม. ตามรังสี
                var dest=target?ray.GetPoint(distance):ray.GetPoint(6);
                // valid = มีจุดหมายและยืนได้ (CanStand)
                bool valid=target && CanStand(dest);
                // วาดเลเซอร์เทเลพอร์ต สีฟ้า (cyan) ถ้าใช้ได้ สีเทาถ้าไม่ได้
                Draw(TeleportPointer,ray.origin,dest,valid?Color.cyan:Color.gray);
                // ถ้า trigger เพิ่งกด, จุดหมายใช้ได้ และไม่ได้ถือของ:
                if(trigger && !previousLeft && valid && !Room.Held)
                {
                    // offset = ระยะระหว่างกล้องกับ rig ในแนวระนาบ (ตัด y)
                    var offset=View.transform.position-transform.position; offset.y=0;
                    // ย้าย rig ให้กล้องไปอยู่ที่จุดหมายพอดี (เทเลพอร์ต)
                    transform.position=dest-offset;
                }
                // จำสถานะ trigger ซ้าย
                previousLeft=trigger;
            }
            // ถ้าไม่ได้ติดตามมือซ้าย รีเซ็ตสถานะ
            else previousLeft=false;
        }
        // CanStand(point): ตรวจว่าผู้เล่นยืนที่จุดนี้ได้หรือไม่ (ไม่ทะลุกำแพง/เฟอร์นิเจอร์/นอกห้อง)
        bool CanStand(Vector3 point)
        {
            // flow = StartScreen
            var flow=Room.StartScreen;
            // ถ้ามีแผนที่:
            if(flow && flow.Maps!=null && flow.Maps.Length>0)
            {
                // boundary = ขอบเขตเดินได้ของแผนที่ที่เลือก
                var boundary=flow.Maps[flow.SelectedMap].WalkBoundary;
                // ถ้ามีขอบเขต และจุดอยู่นอก (เผื่อ 0.3 ม.) → ยืนไม่ได้
                if(boundary!=null && !PlanWalkBoundary.Contains(boundary,new Vector2(point.x,point.z),.3f)) return false;
            }
            // ถ้าห่างศูนย์กลางเกินครึ่งห้องลบ 0.3 ม. ในแกน x หรือ z → ยืนไม่ได้
            if(Mathf.Abs(point.x)>Room.HalfSize.x-.3f || Mathf.Abs(point.z)>Room.HalfSize.y-.3f) return false;
            // ตรวจ collider ใน แคปซูลแทนตัวผู้เล่น (สูง 0.3–1.65 ม. รัศมี 0.25 ม.)
            foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.3f,point+Vector3.up*1.65f,.25f))
                // ถ้าชนสิ่งกีดขวาง (RoomObstacle) → ยืนไม่ได้
                if(collider.GetComponent<RoomObstacle>()) return false;
            // ตรวจเฟอร์นิเจอร์ทุกชิ้น
            foreach(var item in Room.Items)
            {
                // b = ขอบเขตกล่องของชิ้น ขยายออก 0.5 ม. ทุกด้าน
                var b=item.GetComponent<BoxCollider>().bounds; b.Expand(.5f);
                // p = จุดที่ยกให้อยู่ระดับกลางกล่อง; ถ้ากล่องขยายครอบจุดนี้ → ยืนไม่ได้ (ใกล้/ในเฟอร์นิเจอร์เกินไป)
                var p=point; p.y=b.center.y; if(b.Contains(p)) return false;
            }
            // ผ่านทุกข้อ → ยืนได้
            return true;
        }
        // UpdateDesktop(): ควบคุมเมื่อใช้เมาส์/คีย์บอร์ด
        void UpdateDesktop()
        {
            // ปิดเส้นเลเซอร์ VR ทั้งสองเส้น
            Pointer.enabled=false; TeleportPointer.enabled=false;
            // ถ้าอยู่ในเมนูหลัก ไม่ทำอะไร
            if(InMenu) return;
            // ประกาศตัวแปรอินพุต: mouse (ตำแหน่งเมาส์), delta (ระยะเมาส์ขยับ), click, look, rotate, cancel, delete, save, load, move
            Vector2 mouse,delta; bool click,look,rotate,cancel,delete,save,load; Vector2 move;
// ถ้าใช้ Input System ใหม่:
#if ENABLE_INPUT_SYSTEM
            // m = เมาส์, k = คีย์บอร์ด; ถ้าไม่มีให้ออก
            var m=Mouse.current; var k=Keyboard.current; if(m==null || k==null) return;
            // ถ้าเมนูห้องเปิดอยู่ รับเฉพาะ Esc เพื่อปิด แล้วออก
            if(IsRoomMenuOpen) { if(k.escapeKey.wasPressedThisFrame) CloseRoomMenu(); return; }
            // กด Tab = สลับเมนูคลังเฟอร์นิเจอร์
            if(k.tabKey.wasPressedThisFrame && Room.AssetMenu) Room.AssetMenu.Toggle();
            // ถ้าเมนูคลังเปิดอยู่ ไม่ประมวลผลต่อ
            if(Room.AssetMenu && Room.AssetMenu.IsOpen) return;
            // ค้าง Q = ลดความสูงชิ้นที่ถือ, ค้าง E = เพิ่มความสูง (คูณเวลา)
            if(k.qKey.isPressed) Room.Lift(-Time.deltaTime); if(k.eKey.isPressed) Room.Lift(Time.deltaTime);
            // อ่านตำแหน่งเมาส์, ระยะขยับ, คลิกซ้าย (เพิ่งกด), กดค้างคลิกขวา (look)
            mouse=m.position.ReadValue(); delta=m.delta.ReadValue(); click=m.leftButton.wasPressedThisFrame; look=m.rightButton.isPressed;
            // R = หมุน, Esc = ยกเลิก, Delete = ลบ (เพิ่งกด)
            rotate=k.rKey.wasPressedThisFrame; cancel=k.escapeKey.wasPressedThisFrame; delete=k.deleteKey.wasPressedThisFrame;
            // F5 = บันทึก, F9 = โหลด (เพิ่งกด)
            save=k.f5Key.wasPressedThisFrame; load=k.f9Key.wasPressedThisFrame;
            // move = เวกเตอร์เดิน: x = D(+1)/A(-1), y = W(+1)/S(-1)
            move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
// ถ้าไม่ใช่ (ใช้ระบบ Input เก่า):
#else
            // เมนูห้องเปิดอยู่: รับเฉพาะ Esc เพื่อปิด แล้วออก
            if(IsRoomMenuOpen) { if(Input.GetKeyDown(KeyCode.Escape)) CloseRoomMenu(); return; }
            // Tab = สลับเมนูคลังเฟอร์นิเจอร์
            if(Input.GetKeyDown(KeyCode.Tab) && Room.AssetMenu) Room.AssetMenu.Toggle();
            // ถ้าเมนูคลังเปิดอยู่ ไม่ประมวลผลต่อ
            if(Room.AssetMenu && Room.AssetMenu.IsOpen) return;
            // ค้าง Q ลดความสูง, ค้าง E เพิ่มความสูง
            if(Input.GetKey(KeyCode.Q)) Room.Lift(-Time.deltaTime); if(Input.GetKey(KeyCode.E)) Room.Lift(Time.deltaTime);
            // ตำแหน่งเมาส์ และ delta จากแกน Mouse X/Y คูณ 12 (ให้ความไวใกล้เคียงระบบใหม่)
            mouse=Input.mousePosition; delta=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*12;
            // คลิกซ้าย (click), ค้างคลิกขวา (look), R (rotate), Esc (cancel), Delete (delete)
            click=Input.GetMouseButtonDown(0); look=Input.GetMouseButton(1); rotate=Input.GetKeyDown(KeyCode.R); cancel=Input.GetKeyDown(KeyCode.Escape); delete=Input.GetKeyDown(KeyCode.Delete);
            // F5 (save), F9 (load), และ move จากแกน Horizontal/Vertical แบบดิบ
            save=Input.GetKeyDown(KeyCode.F5); load=Input.GetKeyDown(KeyCode.F9); move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
// จบเงื่อนไข #if
#endif
            // หมายเหตุเดิม: IMGUI จัดการการคลิกแถบเครื่องมือภายหลังในเฟรมเดียวกัน จึงต้องไม่หยิบ/วางของที่อยู่ด้านหลังแถบนั้นซ้ำ
            // IMGUI handles toolbar clicks later in the frame; never also pick/place behind it.
            // ถ้าอยู่ในช่วงบล็อกอินพุต หรือเมาส์อยู่ในกรอบแถบปุ่มมุมขวาบน (264×56 พิกเซล แปลงแกน y กลับ) ให้ออก
            if(Time.frameCount<=blockWorldInputThrough || new Rect(Screen.width-264,0,264,56).Contains(new Vector2(mouse.x,Screen.height-mouse.y))) return;
            // ถ้ากดค้างคลิกขวา: หมุนตัวผู้เล่นซ้ายขวาตาม delta.x, ก้ม/เงยตาม delta.y (จำกัด ±75°) แล้วตั้งมุมกล้อง
            if(look) { transform.Rotate(0,delta.x*.12f,0); pitch=Mathf.Clamp(pitch-delta.y*.12f,-75,75); View.transform.localRotation=Quaternion.Euler(pitch,0,0); }
            // step = เวกเตอร์เดินตามทิศขวา/หน้าของผู้เล่น ตัดแกน y
            var step=(transform.right*move.x+transform.forward*move.y); step.y=0;
            // destination = ตำแหน่งปลายทาง: ขยับ ≤ 1 หน่วย × เวลา (จำกัดไม่เกิน 0.05 วิ) × 2 (ความเร็ว 2 ม./วิ)
            var destination=transform.position+Vector3.ClampMagnitude(step,1)*Mathf.Min(Time.deltaTime,.05f)*2;
            // ถ้ายืนที่ปลายทางได้ จึงย้ายไปจริง
            if(CanStand(destination)) transform.position=destination;
            // R = หมุนชิ้นที่ถือ 45°, Esc = ยกเลิก, Delete = ลบ
            if(rotate) Room.Rotate(45); if(cancel) Room.Cancel(); if(delete) Room.Delete();
            // F5 = บันทึก, F9 = โหลด (ผ่าน LayoutStore)
            if(save) Room.GetComponent<LayoutStore>().Save(); if(load) Room.GetComponent<LayoutStore>().Load();
            // เล็งจากตำแหน่งเมาส์ (ทำรังสีผ่านกล้อง) พร้อมส่งสถานะคลิก ไม่มีเลเซอร์
            Point(View.ScreenPointToRay(mouse),click,null);
        }
        // Point(ray,click,line): ประมวลผลการเล็ง — หาสิ่งที่ชน, ย้ายชิ้นที่ถือ, วาดเลเซอร์, และจัดการการคลิก
        void Point(Ray ray,bool click,LineRenderer line)
        {
            // ยิง Raycast ระยะ 15 ม. เก็บผลลง hits
            int count=Physics.RaycastNonAlloc(ray,hits,15,~0,QueryTriggerInteraction.Ignore);
            // closest = ระยะที่ใกล้สุด (เริ่ม 15), target = collider ที่ใกล้สุด
            float closest=15; Collider target=null;
            // วนทุกผลที่ชน
            for(int i=0;i<count;i++)
            {
                // ข้ามชิ้นที่กำลังถืออยู่ (ไม่ให้เล็งชนตัวเอง)
                if(Room.Held && hits[i].collider.gameObject==Room.Held.gameObject) continue;
                // ถ้าใกล้กว่า closest ที่ผ่านมา เก็บเป็น target ใหม่
                if(hits[i].distance<closest) { closest=hits[i].distance; target=hits[i].collider; }
            }
            // button = RoomButton บน target (ถ้ามี)
            var button=target?target.GetComponent<RoomButton>():null;
            // valid = ตำแหน่งที่ถืออยู่ปัจจุบันวางได้หรือไม่ (เริ่ม false)
            bool valid=false;
            // ถ้าถือชิ้นอยู่และไม่ได้เล็งปุ่ม:
            if(Room.Held && !button)
            {
                // plane = ระนาบพื้น
                var plane=new Plane(Vector3.up,Vector3.zero);
                // ถ้ารังสีชนพื้นภายใน 15 ม. และไม่ถูกสิ่งกีดขวางบัง:
                if(plane.Raycast(ray,out float distance) && distance<15 && !Occluded(ray,distance))
                // ย้ายชิ้นไปจุดที่ชนพื้น, ตรวจว่าวางได้ไหมเก็บใน valid, และให้เส้นเลเซอร์ยาวถึงพื้น (closest=distance)
                { Room.Move(ray.GetPoint(distance)); valid=Room.IsValid(Room.Held); closest=distance; }
            }
            // ถ้ามีเส้นเลเซอร์ วาดจากต้นรังสีถึงจุดที่ชน: เขียว = วางได้, แดง = วางไม่ได้ (ตอนถือของ), ฟ้า = ปกติ
            if(line) Draw(line,ray.origin,ray.GetPoint(closest),Room.Held?(valid?Color.green:Color.red):Color.cyan);
            // ถ้าไม่ได้คลิก จบตรงนี้
            if(!click) return;
            // ถ้าเล็งปุ่ม 3 มิติ → ส่งคำสั่งให้ RoomSandbox แล้วจบ
            if(button) { Room.Command(button); return; }
            // door = PlanDoor ที่ target (หรือพ่อแม่ของมัน) ถ้ามี
            var door=target?target.GetComponentInParent<PlanDoor>():null;
            // ถ้าคลิกประตูและไม่ได้ถือของ: ถ้าอยู่ห่างจากประตูไม่เกิน 3 ม. สลับเปิด/ปิดประตู ไม่งั้นแจ้ง 'Move closer to the door'; แล้วจบ
            if(door && !Room.Held) { if(Vector3.Distance(View.transform.position,target.ClosestPoint(View.transform.position))<=3) door.Toggle(); else Room.Message("Move closer to the door");return; }
            // ถ้าถือชิ้นอยู่: ถ้าวางได้ → วาง ไม่งั้นแจ้งให้เล็งพื้นที่โล่งในห้อง; แล้วจบ
            if(Room.Held) { if(valid) Room.Place(); else Room.Message("Aim at a clear floor area inside the room"); return; }
            // ไม่ได้ถืออะไร: หยิบเฟอร์นิเจอร์ที่เล็งอยู่ขึ้นมา (ถ้ามี)
            if(target) Room.Pick(target.GetComponent<FurnitureItem>());
        }
        // Draw(line,from,to,color): ตั้งจุดเริ่ม/จุดจบและสีของเส้นเลเซอร์
        static void Draw(LineRenderer line,Vector3 from,Vector3 to,Color color)
        // ตั้งตำแหน่งจุดที่ 0 และ 1 และสีต้น/สีปลายเป็นสีเดียวกัน
        { line.SetPosition(0,from); line.SetPosition(1,to); line.startColor=color; line.endColor=color; }
        // OnGUI(): วาด UI ส่วนที่อยู่บนจอ
        void OnGUI()
        {
            // ถ้าใช้ VR หรืออยู่หน้าเมนูหลัก ไม่วาด
            if(XRSettings.isDeviceActive || InMenu) return;
            // ปุ่มมุมขวาบน 'MENU' / 'CLOSE MENU' กดแล้วสลับเมนูห้อง
            if(GUI.Button(new Rect(Screen.width-112,12,100,32),IsRoomMenuOpen?"CLOSE MENU":"MENU")) ToggleRoomMenu();
            // ถ้าเมนูห้องเปิด วาดเมนูแล้วจบ
            if(IsRoomMenuOpen) { DrawRoomMenu(); return; }
            // ถ้าเมนูคลังเปิดอยู่ ไม่ต้องวาดแถบสถานะ
            if(Room.AssetMenu && Room.AssetMenu.IsOpen) return;
            // แถบล่างสุด: ข้อความวิธีควบคุม และสถานะปัจจุบันของห้อง
            GUI.Box(new Rect(12,Screen.height-68,Mathf.Max(1,Screen.width-24),56),"WASD move | Right mouse: look | Click: arrange | R rotate | Q/E height\n"+Room.Status);
        }
        // DrawRoomMenu(): วาดเมนูห้อง (กลางจอ)
        void DrawRoomMenu()
        {
            // เก็บ matrix เดิม
            var old=GUI.matrix;
            // scale = min(กว้างจอ/900, สูงจอ/720)
            float scale=Mathf.Min(Screen.width/900f,Screen.height/720f);
            // ตั้ง matrix ให้เลย์เอาต์ 900×720 ย่อ/ขยายตามจอ และจัดกึ่งกลางจอ
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-900*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,new Vector3(scale,scale,1));
            // ตั้งสีพื้นหลัง
            var color=GUI.color;GUI.color=new Color(.06f,.12f,.12f);
            // วาดสี่เหลี่ยมพื้นหลังแล้วคืนสีเดิม
            GUI.DrawTexture(new Rect(90,60,720,630),Texture2D.whiteTexture);GUI.color=color;
            // วาดกรอบหน้าต่างพร้อมหัวข้อ 'HOME / SANDBOX - ROOM MENU'
            GUI.Box(new Rect(90,60,720,630),"HOME / SANDBOX - ROOM MENU");
            // ข้อความแนะนำ
            GUI.Label(new Rect(115,96,665,30),"Arrange your room, save your layout, or return to map selection.");
            // ปุ่ม 'APARTMENT ASSETS': ปิดเมนูห้อง แล้วเปิดเมนูคลังเฟอร์นิเจอร์
            if(GUI.Button(new Rect(115,136,430,42),"APARTMENT ASSETS")) { CloseRoomMenu(); if(Room.AssetMenu) Room.AssetMenu.Toggle(); }
            // ปุ่ม 'RESUME ROOM': ปิดเมนูห้อง กลับไปจัดห้อง
            if(GUI.Button(new Rect(560,136,225,42),"RESUME ROOM")) CloseRoomMenu();
            // ปุ่มแท็บ 'PLACED ASSETS / COSTS' (ติดป้าย [selected] เมื่อแท็บนี้ถูกเลือก) กดแล้วเลือกแท็บนี้
            if(GUI.Button(new Rect(115,184,330,30),showPlacedAssets?"PLACED ASSETS / COSTS  [selected]":"PLACED ASSETS / COSTS")) showPlacedAssets=true;
            // ปุ่มแท็บ 'ADD BASIC FURNITURE' (ติดป้าย [selected] เมื่อเลือก) กดแล้วสลับไปแท็บนี้
            if(GUI.Button(new Rect(455,184,330,30),showPlacedAssets?"ADD BASIC FURNITURE":"ADD BASIC FURNITURE  [selected]")) showPlacedAssets=false;
            // ถ้าเลือกแท็บแรก วาดตารางสรุป (DrawInventory)
            if(showPlacedAssets) DrawInventory();
            // ไม่งั้นแสดงปุ่มเฟอร์นิเจอร์พื้นฐานทุกชิ้น (วนทุกชื่อ):
            else for(int i=0;i<FurnitureCatalog.Names.Length;i++)
            {
                // ปุ่มชื่อ + ราคา เรียง 2 คอลัมน์; กดแล้วเริ่มถือชิ้นนั้นและปิดเมนู
                if(GUI.Button(new Rect(115+(i%2)*340,222+(i/2)*38,330,33),FurnitureCatalog.Names[i]+" | THB "+FurnitureCatalog.Prices[i].ToString("N0"))) { Room.BeginNew(i);CloseRoomMenu(); }
            }
            // ปุ่ม 'SAVE LAYOUT' บันทึกเลย์เอาต์
            if(GUI.Button(new Rect(115,425,215,40),"SAVE LAYOUT")) Room.GetComponent<LayoutStore>().Save();
            // ปุ่ม 'LOAD LAYOUT' โหลดเลย์เอาต์
            if(GUI.Button(new Rect(340,425,215,40),"LOAD LAYOUT")) Room.GetComponent<LayoutStore>().Load();
            // ปุ่ม 'GRID: ON/OFF' สลับการจับกริดแล้วรีเฟรชป้าย
            if(GUI.Button(new Rect(565,425,220,40),Room.Snap?"GRID: ON":"GRID: OFF")) { Room.Snap=!Room.Snap;Room.Refresh(); }
            // ปิดการใช้งานปุ่มด้านล่างถ้าไม่ได้ถือชิ้นอะไรอยู่ (GUI.enabled = มีชิ้นที่ถือหรือไม่)
            GUI.enabled=Room.Held;
            // ปุ่ม 'ROTATE 45'
            if(GUI.Button(new Rect(115,478,215,40),"ROTATE 45")) Room.Rotate(45);
            // ปุ่ม 'CANCEL MOVE'
            if(GUI.Button(new Rect(340,478,215,40),"CANCEL MOVE")) Room.Cancel();
            // ปุ่ม 'DELETE SELECTED'
            if(GUI.Button(new Rect(565,478,220,40),"DELETE SELECTED")) Room.Delete();
            // เปิดการใช้งานปุ่มกลับเป็นปกติ
            GUI.enabled=true;
            // ข้อความสรุป: จำนวนชิ้น | สถานะ
            GUI.Label(new Rect(115,532,670,60),Room.Items.Count+" pieces | "+Room.Status);
            // ปุ่ม 'BACK TO HOME / CHOOSE MAP': ปิดเมนู แล้วกลับหน้าเมนูหลัก
            if(GUI.Button(new Rect(115,605,670,48),"BACK TO HOME / CHOOSE MAP")) { CloseRoomMenu();Room.StartScreen.Show(); }
            // คืน matrix เดิม
            GUI.matrix=old;
        }
        // DrawInventory(): วาดตารางสรุปเฟอร์นิเจอร์ที่วางแล้ว จัดกลุ่มตามชนิดพร้อมราคา
        void DrawInventory()
        {
            // rows = แถวของตาราง, byKey = ตารางค้นหาแถวจาก key (ไว้รวมชิ้นชนิดเดียวกัน)
            var rows=new List<InventoryRow>();var byKey=new Dictionary<string,InventoryRow>();
            // count = จำนวนชิ้นที่วางแล้ว, unknown = จำนวนชิ้นไม่มีราคา, total = ผลรวมราคา (long)
            int count=0,unknown=0;long total=0;
            // วนทุกชิ้นในห้อง
            foreach(var item in Room.Items)
            {
                // ข้ามชิ้นที่ยังไม่ได้วาง (ชิ้นใหม่ที่ยังถืออยู่)
                if(!Room.IsPlaced(item)) continue;
                // key = 'basic:Id' (พื้นฐาน) หรือ 'asset:AssetKey' (จากคลัง)
                string key=string.IsNullOrEmpty(item.AssetKey)?"basic:"+item.Id:"asset:"+item.AssetKey;
                // ถ้ายังไม่มีแถวสำหรับ key นี้:
                if(!byKey.TryGetValue(key,out var row))
                {
                    // สร้างแถวใหม่ (ชื่อชิ้น) และหาราคา (Known = มีราคาหรือไม่)
                    row=new InventoryRow{Name=Room.ItemName(item)};row.Known=Room.TryGetPrice(item,out row.Price);
                    // เก็บแถวเข้าตารางค้นหาและลิสต์
                    byKey.Add(key,row);rows.Add(row);
                }
                // เพิ่มจำนวนในแถวและจำนวนรวม; ถ้ามีราคาบวกเข้ายอดรวม ไม่งั้นนับเป็น unknown
                row.Count++;count++;if(row.Known) total+=row.Price;else unknown++;
            }
            // หัวตาราง: ASSET และ QTY
            GUI.Label(new Rect(125,218,295,26),"ASSET");GUI.Label(new Rect(420,218,55,26),"QTY");
            // หัวตาราง: UNIT / THB และ TOTAL / THB
            GUI.Label(new Rect(480,218,140,26),"UNIT / THB");GUI.Label(new Rect(630,218,130,26),"TOTAL / THB");
            // เริ่มพื้นที่เลื่อนสำหรับตาราง (ความสูงเนื้อหา = 36 พิกเซลต่อแถว อย่างน้อย 123)
            inventoryScroll=GUI.BeginScrollView(new Rect(115,246,670,123),inventoryScroll,new Rect(0,0,645,Mathf.Max(123,rows.Count*36)));
            // ถ้าไม่มีแถวเลย แสดงข้อความ 'No placed assets yet...'
            if(rows.Count==0) GUI.Label(new Rect(10,10,625,40),"No placed assets yet. Choose Apartment Assets or Add Basic Furniture.");
            // วนวาดทุกแถว
            for(int i=0;i<rows.Count;i++)
            {
                // row = แถวที่ i, y = ตำแหน่งแนวตั้ง, เก็บสีเดิมไว้
                var row=rows[i];float y=i*36;var before=GUI.color;
                // วาดพื้นแถวสลับสี (ลายทาง) แล้วคืนสีเดิม
                GUI.color=i%2==0?new Color(.12f,.20f,.20f):new Color(.09f,.16f,.16f);GUI.DrawTexture(new Rect(0,y,645,35),Texture2D.whiteTexture);GUI.color=before;
                // คอลัมน์ชื่อ (พร้อม tooltip) และจำนวน
                GUI.Label(new Rect(10,y+4,285,28),new GUIContent(row.Name,row.Name));GUI.Label(new Rect(305,y+4,50,28),row.Count.ToString());
                // คอลัมน์ราคาต่อหน่วย (ถ้าไม่มีราคาแสดง 'Not priced')
                GUI.Label(new Rect(365,y+4,145,28),row.Known?row.Price.ToString("N0"):"Not priced");
                // คอลัมน์ราคารวมของแถว = ราคา × จำนวน (ใช้ long กันล้น)
                GUI.Label(new Rect(515,y+4,125,28),row.Known?((long)row.Price*row.Count).ToString("N0"):"Not priced");
            }
            // จบพื้นที่เลื่อน
            GUI.EndScrollView();
            // บรรทัดสรุป: จำนวนชิ้นที่วาง | ยอดรวม (ถ้ามีชิ้นไม่มีราคา เปลี่ยนเป็น 'Known-price subtotal' และบอกจำนวนที่ไม่มีราคา)
            GUI.Label(new Rect(125,372,655,24),count+" placed | "+(unknown>0?"Known-price subtotal":"Total")+": THB "+total.ToString("N0")+(unknown>0?" | "+unknown+" not priced":""));
            // หมายเหตุ: ราคาตัวอย่าง (บาท) ชิ้นใหม่นับหลังวางแล้วเท่านั้น
            GUI.Label(new Rect(125,395,655,24),"Sample catalog prices (THB). New items count after placement.");
        }
        // คลาสย่อย InventoryRow = ข้อมูลหนึ่งแถวของตาราง: ชื่อ, จำนวน, ราคาต่อหน่วย, มีราคาหรือไม่
        sealed class InventoryRow { public string Name;public int Count,Price;public bool Known; }
    }
}
