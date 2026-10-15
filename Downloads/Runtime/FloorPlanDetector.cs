// ==== FloorPlanDetector.cs ====
// ตรวจจับ 'กำแพง' จากรูปภาพแปลนพื้น (พื้นหลังขาว เส้นผนังสีเข้ม แนวตั้ง/แนวนอน) แล้วแปลงเป็นรายการสี่เหลี่ยมหน่วยเมตร
// ขั้นตอนหลัก: FindCrop (ตัดขอบ/โลโก้ออก) → Detect (สแกนหาแถบเส้นทึบ) → แนะนำหน้าต่าง/ประตู → สร้าง key ด้วย hash
// นำเข้า System — ใช้ ArgumentException
using System;
// นำเข้า System.Collections.Generic
using System.Collections.Generic;
// นำเข้า UnityEngine — ใช้ Texture2D, Rect, Mathf, JsonUtility
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // คลาส static รวมฟังก์ชันตรวจจับแปลน
    public static class FloorPlanDetector
    {
        // หมายเหตุเดิม: เส้นสถาปัตยกรรมที่ต่อเนื่องกันเป็นตัวกำหนดการตัดขอบ ส่วนตัวอักษรโลโก้ที่แยกกันไม่นับ
        // Connected architectural strokes define the crop; separate logo letters do not.
        // FindCrop(image): คืน Rect (ค่า 0–1 แบบ UV) ของบริเวณที่เป็นแปลนจริงในรูป
        public static Rect FindCrop(Texture2D image)
        {
            // w = 512 (ความกว้างตัวอย่างที่ย่อภาพ), h = ความสูงตามสัดส่วนเดิม
            int w=512,h=Mathf.RoundToInt(w*(float)image.height/image.width);
            // ถ้า h ผิดปกติ (<1 หรือ >2048) คืนทั้งภาพ (Rect 0,0,1,1)
            if(h<1 || h>2048) return new Rect(0,0,1,1);
            // mask = อาร์เรย์บูลีน true = พิกเซลที่เป็น 'เส้น'
            bool[] mask=new bool[w*h];
            // วนทุกพิกเซลของภาพย่อ: สุ่มสีด้วย GetPixelBilinear (พิกัด UV กึ่งกลางพิกเซล)
            // เป็นเส้นเมื่อ ทึบ (alpha>0.5) และ มืด (grayscale<0.4)
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) { Color c=image.GetPixelBilinear((x+.5f)/w,(y+.5f)/h); mask[y*w+x]=c.a>.5f && c.grayscale<.4f; }
            // minX/minY = w/h และ maxX/maxY = 0 เป็นค่าเริ่มต้นกลับด้านของกรอบรวม (bounding box)
            // queue = คิวสำหรับการค้นหาแบบกว้าง (BFS) ขนาดเท่าจำนวนพิกเซล
            int minX=w,minY=h,maxX=0,maxY=0; var queue=new int[w*h];
            // วนทุกพิกเซลในมาสก์ เพื่อหากลุ่มพิกเซลเส้นที่ติดกัน (connected component)
            for(int i=0;i<mask.Length;i++)
            {
                // ข้ามพิกเซลที่ไม่ใช่เส้น; ไม่งั้นเริ่ม BFS: head=0, tail=1, ใส่พิกเซลนี้ในคิว
                // แล้วตั้ง mask[i]=false เพื่อบอกว่าเยี่ยมแล้ว (ไม่ต้องใช้ array visited แยก)
                if(!mask[i]) continue; int head=0,tail=1; queue[0]=i; mask[i]=false;
                // l,r,b,t = ขอบซ้าย/ขวา/ล่าง/บนของกลุ่มนี้ เริ่มที่ตำแหน่งพิกเซลแรก
                int l=i%w,r=l,b=i/w,t=b;
                // วนจนคิวหมด (head ตาม tail ทัน)
                while(head<tail)
                {
                    // ดึงพิกเซลออกจากคิว p แปลงเป็น x,y แล้วขยายกรอบ l,r,b,t ให้ครอบพิกเซลนี้
                    int p=queue[head++],x=p%w,y=p/w; l=Mathf.Min(l,x);r=Mathf.Max(r,x);b=Mathf.Min(b,y);t=Mathf.Max(t,y);
                    // วนเพื่อนบ้าน 8 ทิศ (dy,dx จาก -1 ถึง 1)
                    for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                    // คำนวณพิกัดเพื่อนบ้าน; ถ้านอกภาพข้าม; ถ้าเป็นเส้นอยู่ ให้ทำเครื่องหมายเยี่ยมแล้วและใส่เข้าคิว
                    { int nx=x+dx,ny=y+dy; if(nx<0||nx>=w||ny<0||ny>=h) continue; int n=ny*w+nx; if(mask[n]) { mask[n]=false;queue[tail++]=n; } }
                }
                // กรองกลุ่มที่ไม่ใช่เส้นสถาปัตยกรรมทิ้ง: ถ้ามีพิกเซลน้อยกว่า 35 หรือทั้งกว้างและสูงไม่ถึง 12% ของภาพ
                // (เช่น ตัวอักษรโลโก้) ให้ข้ามไม่นับ
                if(tail<35 || (r-l<w*.12f && t-b<h*.12f)) continue;
                // กลุ่มที่ผ่านเกณฑ์: ขยายกรอบรวมให้ครอบกลุ่มนี้
                minX=Mathf.Min(minX,l);maxX=Mathf.Max(maxX,r);minY=Mathf.Min(minY,b);maxY=Mathf.Max(maxY,t);
            }
            // ถ้าไม่เจออะไรเลย (กรอบไม่ถูกต้อง) คืนทั้งภาพ
            if(minX>=maxX || minY>=maxY) return new Rect(0,0,1,1);
            // ขยายกรอบรวมเผื่อขอบเล็กน้อย (-2/+3 พิกเซล) และจำกัดไม่ให้เกินภาพ
            minX=Mathf.Max(0,minX-2);minY=Mathf.Max(0,minY-2);maxX=Mathf.Min(w,maxX+3);maxY=Mathf.Min(h,maxY+3);
            // แปลงกรอบพิกเซลเป็นสัดส่วน 0–1 ของภาพ แล้วคืนเป็น Rect
            return new Rect(minX/(float)w,minY/(float)h,(maxX-minX)/(float)w,(maxY-minY)/(float)h);
        }
        // Detect(image,metres,cutoff,minimum): ตรวจจับกำแพงทั้งหมด
        // metres = ความกว้างจริงของแปลนที่ครอปแล้ว (เมตร), cutoff = เกณฑ์ความมืดของพิกเซล, minimum = ความยาวกำแพงขั้นต่ำ (เมตร)
        public static FloorPlanImport.Plan Detect(Texture2D image,float metres,float cutoff,float minimum)
        {
            // ตรวจอินพุต: ต้องมีรูป และความกว้างแปลน 3–30 เมตร ไม่เช่นนั้นโยน ArgumentException
            if(!image || metres<3 || metres>30) throw new ArgumentException("Choose an image and a plan width from 3 to 30 m.");
            // หากรอบแปลนจริงในรูป
            Rect crop=FindCrop(image);
            // w = ความกว้างพิกเซลของส่วนที่ครอป จำกัดไม่เกิน 1024
            int w=Mathf.Min(1024,Mathf.RoundToInt(image.width*crop.width));
            // h = ความสูงพิกเซลของส่วนที่ครอป ตามสัดส่วนเดิม อย่างน้อย 1
            int h=Mathf.Max(1,Mathf.RoundToInt(w*image.height*crop.height/(image.width*crop.width)));
            // depth = ความลึกจริงของแปลนเป็นเมตร = metres×h/w
            float depth=metres*h/w;
            // ตรวจ depth ต้อง 3–30 เมตร และ h ไม่เกิน 2048
            if(depth<3 || depth>30 || h>2048) throw new ArgumentException("Cropped plan depth must be 3-30 m.");
            // source = อาร์เรย์บูลีนของพิกเซลเส้นในภาพครอป
            bool[] source=new bool[w*h];
            // เติม source: สุ่มสีที่ตำแหน่งในส่วนที่ครอป เป็นเส้นเมื่อทึบและเข้มกว่า cutoff
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) { var c=image.GetPixelBilinear(crop.x+(x+.5f)/w*crop.width,crop.y+(y+.5f)/h*crop.height); source[y*w+x]=c.a>.5f && c.grayscale<cutoff; }
            // สร้างผลลัพธ์ Plan (width=metres, depth, sourceCrop=crop)
            var result=new FloorPlanImport.Plan{width=metres,depth=depth,sourceCrop=crop};
            // length = ความยาวเส้นขั้นต่ำในหน่วยพิกเซล = minimum×(พิกเซลต่อเมตร) อย่างน้อย 8
            int length=Mathf.Max(8,Mathf.CeilToInt(minimum*w/metres));
            // stroke = ความหนาขั้นต่ำของกำแพงเป็นพิกเซล = 0.3% ของความกว้าง อย่างน้อย 3
            int stroke=Mathf.Max(3,Mathf.RoundToInt(w*.003f));
            // วน 2 แกน: axis 0 = สแกนหาเส้นแนวนอน, axis 1 = สแกนหาเส้นแนวตั้ง (โดยสลับ row/col)
            for(int axis=0;axis<2;axis++)
            {
                // หมายเหตุเดิม: แต่ละทิศทางเห็นพิกเซลต้นฉบับ จึงรักษาจุดต่อรูปตัว T ไว้ได้
                // Each orientation sees the original pixels, preserving T-junctions.
                // ทำสำเนา mask ให้แต่ละแกน; rows/cols สลับกันตามแกน
                var mask=(bool[])source.Clone(); int rows=axis==0?h:w,cols=axis==0?w:h;
                // วนทุกแถว/คอลัมน์
                for(int row=0;row<rows;row++) for(int col=0;col<cols;col++)
                {
                    // ถ้าพิกเซลนี้ไม่ใช่เส้น ข้ามไป (Index แปลง axis,row,col เป็นตำแหน่งในอาร์เรย์)
                    if(!mask[Index(axis,row,col,w)]) continue;
                    // หาจุดสิ้นสุดของแถบเส้นต่อเนื่อง (end = คอลัมน์แรกที่ไม่ใช่เส้น)
                    int end=col; while(end<cols && mask[Index(axis,row,end,w)]) end++;
                    // ถ้าแถบสั้นกว่าความยาวขั้นต่ำ ข้ามไปที่ปลายแถบ (col=end-1 แล้ว continue จะ ++ ต่อ)
                    if(end-col<length) { col=end-1;continue; }
                    // thick = ความหนาของแถบ เริ่มที่ 1 แถว
                    int thick=1;
                    // วนขยายความหนาลงแถวถัดไปเรื่อยๆ
                    while(row+thick<rows)
                    {
                        // นับจำนวนพิกเซลเป็นเส้นในแถวถัดไป ตลอดช่วง col..end
                        int hits=0;for(int c=col;c<end;c++) if(mask[Index(axis,row+thick,c,w)]) hits++;
                        // ถ้าเป็นเส้นน้อยกว่า 94% ของความยาวแถบ หยุดขยาย; ไม่งั้นเพิ่มความหนา
                        if(hits<(end-col)*.94f) break; thick++;
                    }
                    // ถ้าความหนาถึงเกณฑ์ stroke ถือว่าเป็นกำแพง
                    if(thick>=stroke)
                    {
                        // ลบพิกเซลของกำแพงนี้ออกจาก mask เพื่อไม่ให้ตรวจซ้ำ
                        for(int r=row;r<row+thick;r++) for(int c=col;c<end;c++) mask[Index(axis,r,c,w)]=false;
                        // แปลงเป็นสี่เหลี่ยมหน่วยเมตร (จุดกำเนิดอยู่กลางแปลน): แกน 0 ใช้ (col,row) เป็น (x,y); แกน 1 สลับ
                        // ความกว้าง/ความสูงคำนวณจาก (ความยาวแถบ) และ (ความหนา)
                        var box=axis==0?new Rect(col*metres/w-metres/2,row*depth/h-depth/2,(end-col)*metres/w,thick*depth/h):new Rect(row*metres/w-metres/2,col*depth/h-depth/2,thick*metres/w,(end-col)*depth/h);
                        // เพิ่มกำแพงลงรายการ result.walls
                        result.walls.Add(box);
                    }
                    // ข้ามไปท้ายแถบ เพื่อไม่ตรวจพิกเซลในแถบเดิมซ้ำ
                    col=end-1;
                }
            }
            // หมายเหตุเดิม: ลบบล็อกที่ซ้ำ/ถูกบรรจุข้างใน โดยไม่รวมช่องว่างที่อาจเป็นประตู
            // Remove duplicate contained blocks, without joining gaps that may be doors.
            // วนรายการกำแพงจากท้ายไปหน้า (เพื่อลบได้ปลอดภัย)
            for(int i=result.walls.Count-1;i>=0;i--)
                // ถ้ากำแพง i ถูกบรรจุอยู่ในกำแพง j อื่น ให้ลบ i ทิ้ง
                for(int j=0;j<result.walls.Count;j++) if(i!=j && Contains(result.walls[j],result.walls[i])) { result.walls.RemoveAt(i);break; }
            // ถ้าไม่มีกำแพงเลยหรือมากเกิน 300 ชิ้น โยนข้อผิดพลาด (บอกให้ปรับ threshold/ความยาวขั้นต่ำ)
            if(result.walls.Count==0 || result.walls.Count>300) throw new ArgumentException("Detection needs adjustment: "+result.walls.Count+" sections. Change threshold/minimum length.");
            // แนะนำตำแหน่งหน้าต่าง โดยส่งฟังก์ชัน (x,y เมตร) → ค่าความสว่าง 0–1 ของพิกเซลในรูปต้นฉบับ
            // (แปลงเมตรเป็นพิกัดภาพด้วยสัดส่วน crop)
            result.windows=FloorPlanWindows.Suggest(result,(x,y)=>image.GetPixelBilinear(crop.x+(x/metres+.5f)*crop.width,crop.y+(y/depth+.5f)*crop.height).grayscale);
            // แนะนำตำแหน่งประตู ด้วยฟังก์ชันอ่านสีเทาแบบเดียวกัน
            result.doors=FloorPlanDoors.Suggest(result,(x,y)=>image.GetPixelBilinear(crop.x+(x/metres+.5f)*crop.width,crop.y+(y/depth+.5f)*crop.height).grayscale);
            // อัปเดต key ของแปลน แล้วคืนผลลัพธ์
            UpdateKey(result); return result;
        }
        // Index(axis,row,col,w): แปลง (แกน,แถว,คอลัมน์) เป็นดัชนีในอาร์เรย์ 1 มิติ — แกน 0 = row*w+col, แกน 1 = col*w+row
        static int Index(int axis,int row,int col,int w) => axis==0?row*w+col:col*w+row;
        // Contains(a,b): a ครอบ b ทั้งหมดหรือไม่ (เผื่อความคลาดเคลื่อน 1 มม.)
        static bool Contains(Rect a,Rect b) => a.xMin<=b.xMin+.001f && a.yMin<=b.yMin+.001f && a.xMax>=b.xMax-.001f && a.yMax>=b.yMax-.001f;
        // UpdateKey(plan): คำนวณรหัสประจำแปลน
        public static void UpdateKey(FloorPlanImport.Plan plan)
        // ตั้ง key=null ก่อน (ไม่ให้ key เก่ามีผลต่อ hash), แปลงแปลนเป็น JSON, แล้วคำนวณ hash แบบ FNV-1a 32 บิต
        // (ค่าเริ่ม 2166136261, ตัวคูณ 16777619, ใช้ unchecked ให้ล้นได้) ได้ key รูปแบบ 'plan-xxxxxxxx' (เลขฐาน 16 แปดหลัก)
        { plan.key=null; string json=JsonUtility.ToJson(plan); uint hash=2166136261;foreach(char c in json) hash=unchecked((hash^c)*16777619);plan.key="plan-"+hash.ToString("x8"); }
    }
}
