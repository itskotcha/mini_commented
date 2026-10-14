// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้า provider เพื่อใช้ context.watch และ context.read
import 'package:provider/provider.dart';
// นำเข้าข้อมูลจำลอง (findModel, findBrand, colorName)
import '../models/mock_data.dart';
// นำเข้าข้อมูลชิ้นส่วนรองเท้า
import '../models/shoe_parts.dart';
// นำเข้า CustomizerState
import '../state/customizer_state.dart';
// นำเข้าธีมสี
import '../theme/app_theme.dart';
// นำเข้าปุ่มไล่สี
import '../widgets/gradient_button.dart';
// นำเข้าป้ายราคา
import '../widgets/pill_badge.dart';
// นำเข้าตัวแสดงโมเดล 3D
import '../widgets/shoe_model_viewer.dart';

/// หน้า 5: สรุปออเดอร์และยืนยันการสั่งซื้อ
/// แสดงโมเดล 3D พร้อมสี สติกเกอร์ และลายวาดที่ผู้ใช้ออกแบบไว้
// หน้าสรุปออเดอร์ เป็น StatelessWidget
class SummaryScreen extends StatelessWidget {
  // constructor แบบ const
  const SummaryScreen({super.key});

  // เขียนทับ build
  @override
  // build คืนหน้าตาทั้งหน้า
  Widget build(BuildContext context) {
    // watch ทำให้หน้านี้ rebuild ทุกครั้งที่ CustomizerState เปลี่ยน
    final state = context.watch<CustomizerState>();
    // หารุ่นรองเท้าที่เลือกจาก id
    final model = findModel(state.selectedModelId);
    // หาแบรนด์จาก brandId ของรุ่น
    final brand = findBrand(model?.brandId);
    // พาธไฟล์โมเดล 3D ของแบรนด์ ถ้าไม่มีใช้โมเดล Nike
    final assetPath = brand?.glbAsset ?? kShoeAssetNike;
    // รายการชิ้นส่วนที่ตรงกับไฟล์โมเดลนี้
    final parts = partsForAsset(assetPath);

    // Scaffold โครงหน้า
    return Scaffold(
      // แถบหัวเรื่อง สรุปออเดอร์
      appBar: AppBar(title: const Text('สรุปออเดอร์')),
      // SafeArea กันเนื้อหาชนแถบสถานะ/ขอบจอ
      body: SafeArea(
        // คอลัมน์หลัก แบ่งเป็นส่วนเลื่อนด้านบนกับแถบราคาด้านล่าง
        child: Column(
          // รายการลูก
          children: [
            // Expanded ให้ส่วนเลื่อนกินพื้นที่ที่เหลือทั้งหมด
            Expanded(
              // เลื่อนเนื้อหาได้
              child: SingleChildScrollView(
                // ระยะขอบ ซ้าย 20 บน 0 ขวา 20 ล่าง 20
                padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
                // คอลัมน์เนื้อหา
                child: Column(
                  // รายการลูก
                  children: [
                    // ===== โมเดล 3D รองเท้าที่ออกแบบเสร็จ =====
                    // กล่องโมเดล 3D มีขอบไล่สี
                    Container(
                      // กว้างเต็ม
                      width: double.infinity,
                      // สูง 240
                      height: 240,
                      // ตกแต่งขอบ
                      decoration: BoxDecoration(
                        // ไล่สีม่วงเป็นขอบ
                        gradient: AppColors.primaryGradient,
                        // ขอบมน 28
                        borderRadius: BorderRadius.circular(28),
                        // เงา
                        boxShadow: [
                          // เงาหนึ่งชั้น
                          BoxShadow(
                            // ม่วงโปร่งใส 25 เปอร์เซ็นต์
                            color: AppColors.primary.withOpacity(0.25),
                            // เบลอ 30
                            blurRadius: 30,
                            // เลื่อนลง 14
                            offset: const Offset(0, 14),
                          ),
                        ],
                      ),
                      // ระยะขอบใน 3 ทำให้ไล่สีโผล่เป็นขอบบาง ๆ
                      padding: const EdgeInsets.all(3),
                      // ClipRRect ตัดเนื้อหาด้านในให้มนตาม
                      child: ClipRRect(
                        // รัศมี 26
                        borderRadius: BorderRadius.circular(26),
                        // ColoredBox พื้นหลังสีขาว
                        child: ColoredBox(
                          // สีขาว
                          color: Colors.white,
                          // Stack ซ้อนวิดเจ็ต
                          child: Stack(
                            // ให้ลูกขยายเต็ม Stack
                            fit: StackFit.expand,
                            // รายการลูก
                            children: [
                              // ตัวแสดงโมเดล 3D
                              ShoeModelViewer(
                                // ไฟล์โมเดล
                                assetPath: assetPath,
                                // รายการชิ้นส่วน
                                parts: parts,
                                // สีเริ่มต้นของแต่ละชิ้นส่วนจากสถานะ
                                initialColors: state.partColors,
                                // ปิดการหมุนอัตโนมัติ
                                autoRotate: false,
                                // เปิดให้ผู้ใช้หมุนกล้องดูได้ด้วยนิ้ว
                                cameraControls: true,
                                // ส่งรายการสติกเกอร์ไปแสดง
                                stickers: state.stickers,
                                // ส่งรายการเส้นวาดไปแสดง
                                drawingStrokes: state.drawingStrokes,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    // ช่องว่าง 22
                    const SizedBox(height: 22),

                    // แถวแสดงชื่อรุ่นกับราคา
                    Row(
                      // รายการลูก
                      children: [
                        // Expanded ให้คอลัมน์กินพื้นที่
                        Expanded(
                          // คอลัมน์ชื่อกับป้ายราคา
                          child: Column(
                            // จัดชิดซ้าย
                            crossAxisAlignment: CrossAxisAlignment.start,
                            // รายการลูก
                            children: [
                              // ชื่อรุ่น
                              Text(
                                // ถ้าไม่มีรุ่นใช้ข้อความสำรอง
                                model?.name ?? 'รองเท้าที่ออกแบบ',
                                // สไตล์ headlineSmall ตัวหนา
                                style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
                              ),
                              // ช่องว่าง 6
                              const SizedBox(height: 6),
                              // ป้ายราคาเริ่มต้น พร้อมไอคอนป้ายราคา
                              PillBadge(text: 'ราคาเริ่มต้น ${model?.basePrice ?? 0} ฿', icon: Icons.sell_outlined),
                            ],
                          ),
                        ),
                      ],
                    ),

                    // ช่องว่าง 24
                    const SizedBox(height: 24),

                    // Align จัดหัวข้อชิดซ้าย
                    Align(
                      // ชิดกลางซ้าย
                      alignment: Alignment.centerLeft,
                      // ข้อความหัวข้อ
                      child: Text(
                        // สรุปสีที่เลือก
                        'สรุปสีที่เลือก',
                        // สไตล์ titleMedium ตัวหนา
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                      ),
                    ),
                    // ช่องว่าง 10
                    const SizedBox(height: 10),

                    // การ์ดสรุปสี
                    Container(
                      // ตกแต่ง
                      decoration: BoxDecoration(
                        // พื้นขาว
                        color: AppColors.surface,
                        // ขอบมน 22
                        borderRadius: BorderRadius.circular(22),
                        // เงา
                        boxShadow: [
                          // เงาหนึ่งชั้น
                          BoxShadow(
                            // ม่วงโปร่งใส 6 เปอร์เซ็นต์
                            color: AppColors.primary.withOpacity(0.06),
                            // เบลอ 20
                            blurRadius: 20,
                            // เลื่อนลง 8
                            offset: const Offset(0, 8),
                          ),
                        ],
                      ),
                      // ระยะขอบใน 18
                      padding: const EdgeInsets.all(18),
                      // คอลัมน์ของแถวชิ้นส่วน
                      child: Column(
                        // สร้างแถวตามจำนวนชิ้นส่วนด้วย List.generate
                        children: List.generate(shoeParts.length, (i) {
                          // ชิ้นส่วนที่ i
                          final part = shoeParts[i];
                          // สีปัจจุบันของชิ้นส่วนนี้
                          final color = state.colorOf(part.id);
                          // คอลัมน์ของแถวหนึ่งแถวกับตัวคั่น
                          return Column(
                            // รายการลูก
                            children: [
                              // แถวชื่อชิ้นส่วนกับสี
                              Row(
                                // จัดชิดสองด้าน
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                // รายการลูก
                                children: [
                                  // ชื่อชิ้นส่วนด้านซ้าย
                                  Text(part.label, style: Theme.of(context).textTheme.bodyMedium),
                                  // แถวย่อยด้านขวา จุดสีกับชื่อสี
                                  Row(
                                    // รายการลูก
                                    children: [
                                      // วงกลมสีตัวอย่าง
                                      Container(
                                        // กว้าง 20
                                        width: 20,
                                        // สูง 20
                                        height: 20,
                                        // ตกแต่ง
                                        decoration: BoxDecoration(
                                          // สีที่เลือก
                                          color: color,
                                          // รูปวงกลม
                                          shape: BoxShape.circle,
                                          // ขอบดำจางมาก
                                          border: Border.all(color: Colors.black.withOpacity(0.08)),
                                        ),
                                      ),
                                      // ช่องว่าง 10
                                      const SizedBox(width: 10),
                                      // ข้อความชื่อสี
                                      Text(
                                        // ชื่อสีจากพาเลต หรือ กำหนดเอง
                                        colorName(color),
                                        // ดึงสไตล์ bodyMedium ของธีม
                                        style: Theme.of(context)
                                            // ผ่าน textTheme
                                            .textTheme
                                            // bodyMedium
                                            .bodyMedium
                                            // ปรับให้ตัวหนา w600
                                            ?.copyWith(fontWeight: FontWeight.w600),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                              // ถ้ายังไม่ใช่ชิ้นสุดท้าย ให้ใส่ตัวคั่น
                              if (i != shoeParts.length - 1) ...[
                                // ช่องว่าง 12
                                const SizedBox(height: 12),
                                // เส้นคั่น
                                const Divider(height: 1),
                                // ช่องว่าง 12
                                const SizedBox(height: 12),
                              ],
                            ],
                          );
                        }),
                      ),
                    ),
                    // ถ้ามีสติกเกอร์หรือลายวาด แสดงสรุปจำนวน
                    if (state.hasDecorations) ...[
                      // ช่องว่าง 18
                      const SizedBox(height: 18),
                      // กล่องสรุปการตกแต่ง
                      Container(
                        // กว้างเต็ม
                        width: double.infinity,
                        // ระยะขอบใน 16
                        padding: const EdgeInsets.all(16),
                        // ตกแต่ง
                        decoration: BoxDecoration(
                          // พื้นขาว
                          color: AppColors.surface,
                          // ขอบมน 18
                          borderRadius: BorderRadius.circular(18),
                          // ขอบม่วงจาง ๆ
                          border: Border.all(color: AppColors.primary.withOpacity(0.12)),
                        ),
                        // แถวไอคอนกับข้อความ
                        child: Row(
                          // รายการลูก
                          children: [
                            // ไอคอนประกาย
                            const Icon(Icons.auto_awesome_outlined),
                            // ช่องว่าง 12
                            const SizedBox(width: 12),
                            // Expanded ให้ข้อความกินพื้นที่
                            Expanded(
                              // ข้อความ
                              child: Text(
                                // จำนวนสติกเกอร์และจำนวนเส้นวาด
                                'สติกเกอร์ ${state.stickers.length} ชิ้น • ลายวาด ${state.drawingStrokes.length} เส้น',
                                // สไตล์ bodyMedium ตัวหนา
                                style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),

            // ===== แถบราคาสุทธิ + ปุ่ม Checkout (ปักหมุดด้านล่าง) =====
            // แถบราคาสุทธิและปุ่มยืนยัน (ปักหมุดล่างสุดของหน้า)
            Container(
              // ระยะขอบใน ซ้าย 20 บน 16 ขวา 20 ล่าง 20
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
              // ตกแต่ง
              decoration: BoxDecoration(
                // พื้นขาว
                color: AppColors.surface,
                // ขอบมนเฉพาะมุมบน รัศมี 28
                borderRadius: const BorderRadius.only(topLeft: Radius.circular(28), topRight: Radius.circular(28)),
                // เงา
                boxShadow: [
                  // เงาหนึ่งชั้น
                  BoxShadow(
                    // ม่วงโปร่งใส 8 เปอร์เซ็นต์
                    color: AppColors.primary.withOpacity(0.08),
                    // เบลอ 24
                    blurRadius: 24,
                    // เลื่อนขึ้น 8 (ค่าลบ) เพราะแถบอยู่ด้านล่าง
                    offset: const Offset(0, -8),
                  ),
                ],
              ),
              // คอลัมน์
              child: Column(
                // สูงเท่าเนื้อหา
                mainAxisSize: MainAxisSize.min,
                // รายการลูก
                children: [
                  // แถวราคา
                  Row(
                    // จัดชิดสองด้าน
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    // รายการลูก
                    children: [
                      // ป้าย ราคาสุทธิ สีเทา
                      Text('ราคาสุทธิ', style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: AppColors.textGray)),
                      // ตัวเลขราคา
                      Text(
                        // ราคาพื้นฐานของรุ่น (ถ้าไม่มีเป็น 0) ตามด้วยสัญลักษณ์บาท
                        '${model?.basePrice ?? 0} ฿',
                        // สไตล์ headlineSmall
                        style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                              // ตัวหนา
                              fontWeight: FontWeight.bold,
                              // สีม่วงหลัก
                              color: AppColors.primary,
                            ),
                      ),
                    ],
                  ),
                  // ช่องว่าง 14
                  const SizedBox(height: 14),
                  // ปุ่มยืนยันสั่งซื้อ
                  GradientButton(
                    // ข้อความบนปุ่ม
                    label: 'ยืนยันสั่งซื้อ',
                    // ไอคอนถุงช้อปปิ้ง
                    trailingIcon: Icons.shopping_bag_outlined,
                    // เมื่อกดเปิดกล่องข้อความขอบคุณ
                    onPressed: () => _showThankYouDialog(context),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ฟังก์ชันแสดงกล่องข้อความขอบคุณหลังสั่งซื้อ
  void _showThankYouDialog(BuildContext context) {
    // showDialog แสดงหน้าต่างป๊อปอัป
    showDialog(
      // ต้องส่ง context ของหน้า
      context: context,
      // ห้ามแตะนอกกล่องเพื่อปิด ต้องกดปุ่มเท่านั้น
      barrierDismissible: false,
      // ฟังก์ชันสร้างเนื้อหากล่อง
      builder: (dialogContext) {
        // Dialog ตัวกล่อง
        return Dialog(
          // พื้นโปร่งใส เพื่อให้ Container ข้างในกำหนดสีเอง
          backgroundColor: Colors.transparent,
          // Container เนื้อหา
          child: Container(
            // ระยะขอบใน 28
            padding: const EdgeInsets.all(28),
            // ตกแต่ง
            decoration: BoxDecoration(
              // พื้นขาว
              color: AppColors.surface,
              // ขอบมน 28
              borderRadius: BorderRadius.circular(28),
            ),
            // คอลัมน์เนื้อหา
            child: Column(
              // สูงเท่าเนื้อหา
              mainAxisSize: MainAxisSize.min,
              // รายการลูก
              children: [
                // วงกลมไอคอนถูกต้อง
                Container(
                  // กว้าง 72
                  width: 72,
                  // สูง 72
                  height: 72,
                  // ตกแต่ง
                  decoration: BoxDecoration(
                    // ไล่สีม่วง
                    gradient: AppColors.primaryGradient,
                    // รูปวงกลม
                    shape: BoxShape.circle,
                  ),
                  // จัดกึ่งกลาง
                  alignment: Alignment.center,
                  // ไอคอนเครื่องหมายถูกสีขาว ขนาด 36
                  child: const Icon(Icons.check_rounded, color: Colors.white, size: 36),
                ),
                // ช่องว่าง 20
                const SizedBox(height: 20),
                // ข้อความหัวข้อ
                Text(
                  // ขอบคุณสำหรับการสั่งซื้อ
                  'ขอบคุณสำหรับการสั่งซื้อ!',
                  // จัดกึ่งกลาง
                  textAlign: TextAlign.center,
                  // สไตล์ titleLarge ตัวหนา w700
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                ),
                // ช่องว่าง 10
                const SizedBox(height: 10),
                // ข้อความรายละเอียด
                Text(
                  // ข้อความสองบรรทัดแจ้งว่าบันทึกคำสั่งซื้อแล้ว
                  'คำสั่งซื้อของคุณถูกบันทึกเรียบร้อยแล้ว\nทีมงานจะเริ่มผลิตรองเท้าตามแบบที่คุณออกแบบไว้',
                  // จัดกึ่งกลาง
                  textAlign: TextAlign.center,
                  // สไตล์ bodyMedium สีเทา
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: AppColors.textGray),
                ),
                // ช่องว่าง 24
                const SizedBox(height: 24),
                // ปุ่มกลับหน้าแรก
                GradientButton(
                  // ข้อความบนปุ่ม
                  label: 'กลับหน้าแรก',
                  // ความสูง 50
                  height: 50,
                  // เมื่อกด
                  onPressed: () {
                    // ปิดกล่องข้อความ (ใช้ dialogContext)
                    Navigator.of(dialogContext).pop();
                    // รีเซ็ตการออกแบบทั้งหมดผ่าน context.read (อ่านครั้งเดียวไม่ฟังการเปลี่ยนแปลง)
                    context.read<CustomizerState>().resetDesign();
                    // ไปหน้า /brands และล้าง backstack ทั้งหมดเพื่อเริ่มออเดอร์ใหม่
                    Navigator.of(context).pushNamedAndRemoveUntil('/brands', (route) => false);
                  },
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
