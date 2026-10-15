// ==== FloorPlanDoors.cs ====
// (1) PlanDoorData = ข้อมูลของประตู 1 บาน + สูตรเรขาคณิต (จุดบานพับ, ส่วนโค้งการเหวี่ยง, มุมของบาน)
// (2) FloorPlanDoors = ตัวแนะนำประตู: หาช่องว่างในกำแพงที่น่าจะเป็นประตู แล้วเช็คว่าในรูปมี 'เส้นโค้งการเหวี่ยง' กับ 'เส้นบาน' หรือไม่
// นำเข้า System — ใช้ [Serializable], Func
using System;
// นำเข้า System.Collections.Generic
using System.Collections.Generic;
// นำเข้า UnityEngine
using UnityEngine;
// ขอบเขตชื่อ HomeSandbox
namespace HomeSandbox
{
    // ข้อมูลประตู 1 บาน ([Serializable] เพื่อบันทึกเป็น JSON ไปกับแปลนได้)
    [Serializable] public sealed class PlanDoorData
    {
        // opening = สี่เหลี่ยมของช่องเปิดบนแปลน (หน่วยเมตร)
        public Rect opening;
        // hingeAtEnd = บานพับอยู่ที่ปลายด้านค่ามาก (xMax/yMax) หรือไม่; positiveSide = บานเหวี่ยงไปด้านบวกของแกนตั้งฉาก (ค่าเริ่มต้น true)
        public bool hingeAtEnd,positiveSide=true;
        // confirmed = ผู้ใช้กด Accept ยืนยันประตูนี้แล้ว (เฉพาะที่ยืนยันแล้วจึงถูกสร้างเป็นประตูจริง)
        public bool confirmed;
        // หมายเหตุเดิม: ความกว้างช่องกรอบที่ใช้ได้จริงเป็นเมตร ไม่ขึ้นกับการขยายแผนที่; ค่า 0 = ใช้ค่าเริ่มต้น
        // Clear frame span in metres, independent of map enlargement. Zero uses the default.
        // passageWidth = ความกว้างช่องกรอบประตูที่ผู้ใช้ตั้ง
        public float passageWidth;
        // Horizontal: ช่องเปิดเป็นแนวนอนหรือไม่ (กว้าง ≥ สูง)
        public bool Horizontal => opening.width>=opening.height;
        // Length: ความยาวของช่องเปิดตามแนวที่ยาวกว่า
        public float Length => Horizontal?opening.width:opening.height;
        // PassageWidth: ความกว้างช่องที่ใช้งานจริง = min(Length, passageWidth ถ้ามากกว่า 0 ไม่งั้น 1.1 ม.)
        public float PassageWidth => Mathf.Min(Length,passageWidth>0?passageWidth:1.1f);
        // Passage: สี่เหลี่ยมของช่องเดินผ่านจริง (ส่วนที่เหลือของช่องเปิดจะถูกเติมเป็นกำแพง)
        public Rect Passage
        {
            // คำนวณจากช่องเปิด: ถ้าแนวนอน — ถ้าบานพับอยู่ปลาย ให้ขยับ x ไปชิดปลายขวา แล้วตั้ง width=PassageWidth
            // ถ้าแนวตั้งทำเช่นเดียวกันกับ y/height แล้วคืน Rect
            get { var r=opening;if(Horizontal) { if(hingeAtEnd) r.x=r.xMax-PassageWidth;r.width=PassageWidth; }else { if(hingeAtEnd) r.y=r.yMax-PassageWidth;r.height=PassageWidth; }return r; }
        }
        // Hinge: ตำแหน่งจุดบานพับ (ปลายซ้าย/บน หรือปลายขวา/ล่างของช่อง ที่กึ่งกลางความหนา)
        public Vector2 Hinge => Horizontal?new Vector2(hingeAtEnd?opening.xMax:opening.xMin,opening.center.y):new Vector2(opening.center.x,hingeAtEnd?opening.yMax:opening.yMin);
        // Arc(angle): จุดบนเส้นโค้งที่ปลายบานเคลื่อนผ่าน เมื่อบานเปิดมุม angle (เรเดียน; 0 = ปิด, π/2 = เปิด 90°)
        public Vector2 Arc(float angle)
        {
            // along = ระยะตามแนวช่องเปิด = ±Length×cos(angle); side = ระยะตั้งฉาก = ±Length×sin(angle) (เครื่องหมายขึ้นกับด้านบานพับ/ด้านที่เหวี่ยง)
            float along=(hingeAtEnd?-1:1)*Length*Mathf.Cos(angle),side=(positiveSide?1:-1)*Length*Mathf.Sin(angle);
            // คืนจุด = Hinge + เวกเตอร์ (along,side) — ถ้าแนวตั้งให้สลับแกนเป็น (side,along)
            return Hinge+(Horizontal?new Vector2(along,side):new Vector2(side,along));
        }
        // BuiltArc(angle): เหมือน Arc แต่ย่อให้เท่าความกว้างช่องจริง (PassageWidth/Length) — ใช้วาดเส้นโค้งของประตูที่จะสร้างจริง
        public Vector2 BuiltArc(float angle) => Hinge+(Arc(angle)-Hinge)*(PassageWidth/Length);
        // ClosedYaw: มุมหมุนรอบแกนตั้ง (องศา) ของ GameObject ประตู ตอนบานปิด — ทำให้แกน X ของประตูวิ่งไปตามช่องเปิด
        public float ClosedYaw => (Horizontal?0:-90)+(hingeAtEnd?180:0);
        // SwingYaw: มุม (องศา) ที่ pivot หมุนไปเมื่อบานเปิดเต็ม ขึ้นกับแนวช่อง ด้านบานพับ และด้านที่เหวี่ยง
        public float SwingYaw => (Horizontal?-90:90)*(hingeAtEnd?-1:1)*(positiveSide?1:-1);
    }
    // คลาส static ตัวแนะนำประตู
    public static class FloorPlanDoors
    {
        // Suggest(plan,grey): คืนรายการประตูที่แนะนำ; grey(x,y) คือฟังก์ชันอ่านความสว่างของรูปต้นฉบับ ณ ตำแหน่งเมตร
        public static List<PlanDoorData> Suggest(FloorPlanImport.Plan plan,Func<float,float,float> grey)
        {
            // result = รายการผลลัพธ์
            var result=new List<PlanDoorData>();
            // วน 2 แกน (0 = กำแพงแนวนอน, 1 = แนวตั้ง) และทุกกำแพงในแปลน
            for(int axis=0;axis<2;axis++) foreach(var wall in plan.walls)
            {
                // ข้ามกำแพงที่ไม่ได้ยาวเป็นแนวเส้นตรงตามแกนนี้ (ต้องยาวกว่าความหนาอย่างน้อย 2 เท่า)
                if(axis==0?wall.width<wall.height*2:wall.height<wall.width*2) continue;
                // line = พิกัดกึ่งกลางของกำแพงตามแกนตั้งฉาก (เส้นที่จะตรวจหาช่องว่างตามแนวนี้)
                float line=axis==0?wall.center.y:wall.center.x;
                // spans = ช่วงของกำแพง (ปลายซ้าย–ปลายขวา) ที่อยู่บนเส้นเดียวกันนี้
                var spans=new List<Vector2>();
                // วนทุกกำแพง r เพื่อหาตัวที่พาดผ่านเส้น line
                foreach(var r in plan.walls)
                {
                    // lo/hi = ช่วงตามแกนตั้งฉากของ r
                    float lo=axis==0?r.yMin:r.xMin,hi=axis==0?r.yMax:r.xMax;
                    // ถ้า r คร่อมเส้น line อยู่ ให้เพิ่มช่วงตามแนวเส้น (xMin–xMax หรือ yMin–yMax) ลง spans
                    if(lo<=line && hi>=line) spans.Add(axis==0?new Vector2(r.xMin,r.xMax):new Vector2(r.yMin,r.yMax));
                }
                // เรียง spans จากน้อยไปมาก; ถ้ามีน้อยกว่า 2 ช่วง (ไม่มีช่องว่างระหว่างกัน) ข้าม
                spans.Sort((a,b)=>a.x.CompareTo(b.x));if(spans.Count<2) continue;
                // end = ตำแหน่งปลายสุดของส่วนกำแพงที่ผ่านมาแล้ว (เริ่มจากช่วงแรก)
                float end=spans[0].y;
                // วนช่วงถัดไปทีละช่วง
                for(int i=1;i<spans.Count;i++)
                {
                    // start = จุดเริ่มของช่วงนี้, gap = ช่องว่างระหว่าง end กับ start
                    float start=spans[i].x,gap=start-end;
                    // ถ้า gap อยู่ระหว่าง 0.55–1.6 ม. ถือว่าอาจเป็นช่องประตู
                    if(gap>=.55f && gap<=1.6f)
                    {
                        // สร้างสี่เหลี่ยมช่องเปิด (หนา 0.08 ม. ครอบเส้น line) ตามแกนของมัน
                        var opening=axis==0?new Rect(end,line-.04f,gap,.08f):new Rect(line-.04f,end,.08f,gap);
                        // overlaps = ช่องนี้ซ้อนกับของอื่นหรือไม่ (เริ่ม false)
                        bool overlaps=false;
                        // ถ้าซ้อนกับหน้าต่างที่แนะนำไว้แล้ว → overlaps = true
                        if(plan.windows!=null) foreach(var r in plan.windows) if(r.Overlaps(opening)) overlaps=true;
                        // ถ้าศูนย์กลางห่างจากประตูที่เจอแล้วน้อยกว่า 0.3 ม. (กำลังสอง < 0.09) ถือเป็นประตูซ้ำ
                        foreach(var d in result) if((d.opening.center-opening.center).sqrMagnitude<.09f) overlaps=true;
                        // ถ้าไม่ซ้อนอะไร จึงประเมินต่อ
                        if(!overlaps)
                        {
                            // best/bestScore = ตัวเลือกที่ดีที่สุดและคะแนน
                            PlanDoorData best=null;int bestScore=0;
                            // ลองทุกชุดผสม: ด้านบานพับ (2) × ด้านที่เหวี่ยง (2) = 4 แบบ
                            for(int hinge=0;hinge<2;hinge++) for(int side=0;side<2;side++)
                            {
                                // สร้างประตูสมมติแบบนั้นเพื่อทดสอบ
                                var door=new PlanDoorData{opening=opening,hingeAtEnd=hinge==1,positiveSide=side==1};
                                // arc = คะแนนความตรงกับเส้นโค้งในรูป, leaf = คะแนนความตรงกับเส้นตัวบานประตู
                                int arc=ArcScore(door,grey),leaf=LeafScore(door,grey);
                                // คะแนนรวม = arc+leaf เฉพาะเมื่อ arc ≥ 8 และ leaf ≥ 22 (เกณฑ์ขั้นต่ำ) ไม่งั้น 0
                                int score=arc>=8 && leaf>=22?arc+leaf:0;
                                // ถ้าคะแนนดีที่สุดจนถึงตอนนี้ จำไว้
                                if(score>bestScore) { bestScore=score;best=door; }
                            }
                            // ถ้าเจอแบบที่ผ่านเกณฑ์ และจำนวนประตูยังไม่ถึง 60 ให้เพิ่มเข้า result
                            if(best!=null && result.Count<60) result.Add(best);
                        }
                    }
                    // อัปเดต end ให้เป็นปลายไกลสุดของกำแพงที่ผ่านมา
                    end=Mathf.Max(end,spans[i].y);
                }
            }
            // คืนรายการประตูที่แนะนำ
            return result;
        }
        // ArcScore(door,grey): ให้คะแนนว่าในรูปมีเส้นโค้งการเหวี่ยงของประตูแบบนี้กี่จุด (สูงสุด 48)
        static int ArcScore(PlanDoorData door,Func<float,float,float> grey)
        {
            // hits = ตัวนับจุดที่ตรง
            int hits=0;
            // ตรวจ 48 จุดตามเส้นโค้ง
            for(int i=0;i<48;i++)
            {
                // p = จุดบนเส้นโค้งที่มุม 0.15 ถึง 1.40 เรเดียน (แบ่งเท่าๆ กัน); radial = เวกเตอร์ทิศออกจากบานพับ (ทิศรัศมี)
                Vector2 p=door.Arc(.15f+i*1.25f/47),radial=(p-door.Hinge).normalized;
                // centre = ค่าความสว่างต่ำสุด (เริ่ม 1 = ขาว)
                float centre=1;
                // สุ่มอ่านความสว่าง 3 จุดตามทิศรัศมี (j = -1,0,1) รอบ p แล้วเก็บค่าที่มืดที่สุด = ค่ากลางของเส้น
                for(int j=-1;j<=1;j++) { var q=p+radial*j*door.Length*.014f;centre=Mathf.Min(centre,grey(q.x,q.y)); }
                // a,b = จุดอยู่ด้านในและด้านนอกของเส้น ห่างจาก p ตามทิศรัศมี
                var a=p-radial*door.Length*.055f;var b=p+radial*door.Length*.055f;
                // นับเป็น hit ถ้าเส้นกลางเข้ม (<0.8) และเข้มกว่าค่าเฉลี่ยของสองด้านอย่างน้อย 0.065 (แปลว่าเป็นเส้นบางๆ ที่โดดเด่นจากพื้น)
                if(centre<.8f && centre+.065f<(grey(a.x,a.y)+grey(b.x,b.y))*.5f) hits++;
            }
            // คืนจำนวน hit
            return hits;
        }
        // LeafScore(door,grey): ให้คะแนนว่าในรูปมี 'เส้นตัวบาน' ประตูที่เปิด 90° กี่จุด (สูงสุด 32)
        public static int LeafScore(PlanDoorData door,Func<float,float,float> grey)
        {
            // hits=0; direction = ทิศของบานเมื่อเปิด 90° (จากบานพับถึงปลายบาน) แบบนอร์มัลไลซ์; side = เวกเตอร์ตั้งฉากกับทิศนั้น
            int hits=0;var direction=(door.Arc(Mathf.PI/2)-door.Hinge).normalized;var side=new Vector2(-direction.y,direction.x);
            // ตรวจ 32 จุด
            for(int i=0;i<32;i++)
            {
                // p = จุดตามตัวบาน ตั้งแต่ 15% ถึง 85% ของความยาว; centre เริ่มที่ 1
                var p=door.Hinge+direction*door.Length*(.15f+i*.7f/31);float centre=1;
                // สุ่ม 7 จุดตั้งฉากกับบาน (j=-3..3) เก็บค่าความสว่างต่ำสุดเป็นเส้นกลาง
                for(int j=-3;j<=3;j++) { var q=p+side*j*door.Length*.014f;centre=Mathf.Min(centre,grey(q.x,q.y)); }
                // a,b = จุดข้างซ้ายและขวาของเส้น ห่าง 8% ของความยาวช่อง
                var a=p-side*door.Length*.08f;var b=p+side*door.Length*.08f;
                // นับเป็น hit ถ้าเส้นกลางเข้ม (<0.8) และเข้มกว่าค่าเฉลี่ยสองข้างอย่างน้อย 0.08
                if(centre<.8f && centre+.08f<(grey(a.x,a.y)+grey(b.x,b.y))*.5f) hits++;
            }
            // คืนจำนวน hit
            return hits;
        }
    }
}
