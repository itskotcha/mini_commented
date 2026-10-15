// ==== FloorPlanWindows.cs ====
// (1) Suggest: แนะนำตำแหน่งหน้าต่างจากรูปแปลน — หาช่องว่างยาวๆ ในกำแพงด้านนอกที่มีเส้นขอบกรอบคู่ขนานขนาบอยู่
// (2) Build: สร้างโมเดล 3 มิติของหน้าต่าง (กำแพงใต้/เหนือหน้าต่าง, กระจก, กรอบ, คิ้ว) ในฉาก
// นำเข้า System — ใช้ Func
using System;
// นำเข้า System.Collections.Generic
using System.Collections.Generic;
// นำเข้า UnityEngine
using UnityEngine;

// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส static รวมฟังก์ชันเกี่ยวกับหน้าต่าง
    public static class FloorPlanWindows
    {
        // หมายเหตุเดิม: แนะนำเฉพาะช่องว่างที่มีกรอบบนผนังด้านนอก; ช่องเปิดด้านในปล่อยไว้ไม่จัดประเภท
        // Only suggest framed gaps on the exterior. Interior openings stay unclassified.
        // Suggest(plan,grey): คืนรายการ Rect ของหน้าต่างที่แนะนำ; grey(x,y) อ่านความสว่างของรูป ณ ตำแหน่งเมตร
        public static List<Rect> Suggest(FloorPlanImport.Plan plan, Func<float,float,float> grey)
        {
            // result = รายการผลลัพธ์
            var result=new List<Rect>();
            // วน 4 ด้านของแปลน: axis 0 = ด้านบน/ล่าง, axis 1 = ด้านซ้าย/ขวา; side 0 = ด้านค่าน้อย, side 1 = ด้านค่ามาก
            for(int axis=0;axis<2;axis++) for(int side=0;side<2;side++)
            {
                // extent = ขนาดแปลนในแกนตั้งฉากกับด้านนี้ (depth หรือ width)
                float extent=axis==0?plan.depth:plan.width;
                // boundary = ตำแหน่งขอบนอกสุดของกำแพง (เริ่มที่ ±ค่าสูงสุดตามด้าน)
                float boundary=side==0?float.MaxValue:float.MinValue;
                // วนทุกกำแพงเพื่อหาขอบนอกสุดของด้านนี้
                foreach(var r in plan.walls)
                {
                    // v = ขอบของกำแพง r ที่ด้านนี้ (yMin/yMax หรือ xMin/xMax)
                    float v=axis==0?(side==0?r.yMin:r.yMax):(side==0?r.xMin:r.xMax);
                    // อัปเดต boundary ให้เป็นค่าต่ำสุด (ด้าน 0) หรือสูงสุด (ด้าน 1)
                    boundary=side==0?Mathf.Min(boundary,v):Mathf.Max(boundary,v);
                }
                // inset = ระยะเข้าไปด้านใน 2.6% ของ extent
                float inset=extent*.026f;
                // line = เส้นขนานกับขอบนอก ที่ลึกเข้าไป inset (ใช้ตรวจหาแนวกำแพง)
                float line=boundary+(side==0?inset:-inset);
                // spans = ช่วงของกำแพงที่อยู่ในแถบแนวขอบนอกนี้
                var spans=new List<Vector2>();
                // วนทุกกำแพง r
                foreach(var r in plan.walls)
                {
                    // lo/hi = ช่วงตามแกนตั้งฉากของ r
                    float lo=axis==0?r.yMin:r.xMin,hi=axis==0?r.yMax:r.xMax;
                    // ถ้า r ซ้อนกับแถบระหว่าง boundary กับ line ให้เพิ่มช่วงตามแนวขอบ (xMin–xMax หรือ yMin–yMax) ลง spans
                    if(lo<=Mathf.Max(boundary,line) && hi>=Mathf.Min(boundary,line)) spans.Add(axis==0?new Vector2(r.xMin,r.xMax):new Vector2(r.yMin,r.yMax));
                }
                // เรียง spans จากน้อยไปมาก
                spans.Sort((a,b)=>a.x.CompareTo(b.x));
                // ถ้ามีน้อยกว่า 2 ช่วง (ไม่มีช่องว่าง) ข้ามด้านนี้
                if(spans.Count<2) continue;
                // end = ปลายสุดของกำแพงช่วงที่ผ่านมา (เริ่มที่ช่วงแรก)
                float end=spans[0].y;
                // วนช่วงถัดไป
                for(int i=1;i<spans.Count;i++)
                {
                    // start = จุดเริ่มของช่วงนี้, gap = ช่องว่างระหว่างช่วงก่อนหน้ากับช่วงนี้
                    float start=spans[i].x, gap=start-end;
                    // หมายเหตุเดิม: ช่องแคบกำกวม (ธรณีประตูก็มีเส้นคู่ได้)
                    // Narrow gaps are ambiguous (door thresholds can also have paired strokes).
                    // span = ความยาวทั้งด้านตามแนวนี้
                    float span=axis==0?plan.width:plan.depth;
                    // รับเฉพาะ gap ที่กว้างกว่า max(0.3 ม., 12% ของด้าน) และแคบกว่า 40% ของด้าน
                    if(gap>Mathf.Max(.3f,span*.12f) && gap<span*.4f)
                    {
                        // หมายเหตุเดิม: ต้องเป็นเส้นยาวสองเส้นแยกกัน ไม่ใช่ช่องเดี่ยวที่เกิดจาก threshold
                        // Two separated long strokes, not a single thresholded gap.
                        // bands = ตำแหน่งเริ่มของแถบเส้นเข้มแต่ละแถบ; inside = ตอนนี้อยู่ในแถบเข้มหรือไม่
                        var bands=new List<float>(); bool inside=false;
                        // step = ขั้นสแกน = extent/1024
                        float step=extent/1024f;
                        // สแกน offset ตั้งแต่ -inset ถึง +inset รอบเส้น line เพื่อหาเส้นกรอบ
                        for(float offset=-inset;offset<=inset;offset+=step)
                        {
                            // hits = จำนวนจุดเข้มบน offset นี้
                            int hits=0;
                            // สุ่ม 80 จุดตามความกว้างของช่องว่าง
                            for(int s=0;s<80;s++)
                            {
                                // along = ตำแหน่งตามช่อง ไล่จาก end ถึง start (กึ่งกลางของแต่ละ 1/80)
                                float along=Mathf.Lerp(end,start,(s+.5f)/80f);
                                // ถ้าความสว่างที่จุดนั้น < 0.72 ถือว่าเข้ม (กลับแกน x/y ตามด้าน)
                                if((axis==0?grey(along,line+offset):grey(line+offset,along))<.72f) hits++;
                            }
                            // dark = มีจุดเข้มอย่างน้อย 70 จาก 80 (≈87.5%) = เป็นเส้นยาวต่อเนื่อง
                            bool dark=hits>=70;
                            // ถ้าเพิ่งเข้าแถบเข้มใหม่ (dark และไม่ได้อยู่ในแถบ) บันทึกตำแหน่งลง bands
                            if(dark && !inside) bands.Add(line+offset);
                            // อัปเดตสถานะ inside
                            inside=dark;
                        }
                        // ถ้าเจออย่างน้อย 2 แถบ (เส้นกรอบคู่) ถือเป็นหน้าต่าง
                        if(bands.Count>=2)
                        {
                            // centre = กึ่งกลางระหว่างแถบแรกกับแถบสุดท้าย
                            float centre=(bands[0]+bands[bands.Count-1])*.5f;
                            // เพิ่ม Rect หน้าต่าง (หนา 0.12 ม.) ตามแกนของมัน ยาวเท่า gap
                            result.Add(axis==0?new Rect(end,centre-.06f,gap,.12f):new Rect(centre-.06f,end,.12f,gap));
                        }
                    }
                    // อัปเดต end ให้เป็นปลายไกลสุดที่ผ่านมา
                    end=Mathf.Max(end,spans[i].y);
                }
            }
            // คืนรายการหน้าต่าง
            return result;
        }

        // Build(root,r,wall,frame,glass): สร้างหน้าต่าง 3 มิติใต้ root ตามสี่เหลี่ยม r พร้อมวัสดุกำแพง/กรอบ/กระจก
        public static void Build(GameObject root, Rect r, Material wall, Material frame, Material glass)
        {
            // horizontal = หน้าต่างอยู่แนวนอนหรือไม่ (กว้าง ≥ สูง)
            bool horizontal=r.width>=r.height;
            // length = ความยาวหน้าต่างตามแนวที่ยาวกว่า
            float length=horizontal?r.width:r.height;
            // สร้าง GameObject 'Plan window' เป็นลูกของ root
            var window=new GameObject("Plan window"); window.transform.SetParent(root.transform,false);
            // วางที่ศูนย์กลางของ Rect (Rect.y แปลงเป็นแกน Z ของโลก)
            window.transform.localPosition=new Vector3(r.center.x,0,r.center.y);
            // ถ้าเป็นแนวตั้ง หมุน 90° รอบแกนตั้ง ให้แกน X ของหน้าต่างวิ่งตามความยาว
            if(!horizontal) window.transform.localRotation=Quaternion.Euler(0,90,0);
            // ส่วนกำแพงใต้หน้าต่าง (สูง 0.9 ม. จากพื้น)
            Part(window,"Sill wall",0,.45f,length,.9f,.12f,wall);
            // คานเหนือหน้าต่าง (lintel) ช่วงสูง 2.5–2.8 ม.
            Part(window,"Lintel",0,2.65f,length,.3f,.12f,wall);
            // แผ่นกระจก (บางมาก 2.5 ซม.) ช่วงสูง 0.9–2.5 ม.
            Part(window,"Glass",0,1.7f,length,1.6f,.025f,glass);
            // กรอบล่างของกระจก
            Part(window,"Bottom frame",0,.94f,length,.08f,.14f,frame);
            // กรอบบนของกระจก
            Part(window,"Top frame",0,2.46f,length,.08f,.14f,frame);
            // กรอบข้างซ้าย
            Part(window,"Left frame",-length/2+.035f,1.7f,.07f,1.6f,.14f,frame);
            // กรอบข้างขวา
            Part(window,"Right frame",length/2-.035f,1.7f,.07f,1.6f,.14f,frame);
            // คิ้ว/เสากลาง (mullion) แบ่งกระจกเป็นสองบาน
            Part(window,"Mullion",0,1.7f,.045f,1.6f,.14f,frame);
        }
        // Part(...): ฟังก์ชันช่วยสร้างชิ้นส่วนกล่อง: ตำแหน่ง (x,y,0), ขนาด (w,h,d)
        static void Part(GameObject root,string name,float x,float y,float w,float h,float d,Material material)
        {
            // สร้างกล่องด้วย FurnitureCatalog.Part (ซึ่งปิด collider ไว้)
            var go=FurnitureCatalog.Part(root,name,new Vector3(x,y,0),new Vector3(w,h,d),material);
            // เปิด collider และติดป้าย RoomObstacle ให้เป็นสิ่งกีดขวางจริง
            go.GetComponent<Collider>().enabled=true; go.AddComponent<RoomObstacle>();
        }
    }
}
