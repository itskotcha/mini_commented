// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าธีมเพื่อใช้ AppColors
import '../theme/app_theme.dart';

/// ปุ่มไล่เฉดสีม่วง ทรงพิล ใช้แทนปุ่มหลักในทุกหน้า (Next / Checkout ฯลฯ)
// ปุ่มหลักแบบไล่สี เป็น StatelessWidget
class GradientButton extends StatelessWidget {
  // ข้อความบนปุ่ม
  final String label;
  // ฟังก์ชันที่เรียกเมื่อกด (null จะทำให้ปุ่มกดไม่ได้)
  final VoidCallback? onPressed;
  // ไอคอนท้ายปุ่ม (ไม่บังคับ)
  final IconData? trailingIcon;
  // ความสูงปุ่ม
  final double height;

  // constructor แบบ const
  const GradientButton({
    // ส่งต่อ key
    super.key,
    // label ต้องระบุ
    required this.label,
    // onPressed ต้องระบุ (แต่เป็น null ได้)
    required this.onPressed,
    // trailingIcon ไม่บังคับ
    this.trailingIcon,
    // height ค่าเริ่มต้น 56
    this.height = 56,
  });

  // เขียนทับ build
  @override
  // build คืนหน้าตาปุ่ม
  Widget build(BuildContext context) {
    // กล่องกำหนดขนาดปุ่ม
    return SizedBox(
      // กว้างเต็มพื้นที่ที่มี
      width: double.infinity,
      // สูงตามพารามิเตอร์ height
      height: height,
      // DecoratedBox วาดพื้นหลังไล่สีและเงา
      child: DecoratedBox(
        // กำหนดการตกแต่ง
        decoration: BoxDecoration(
          // ใช้ gradient หลักของแอป
          gradient: AppColors.primaryGradient,
          // ขอบมนรัศมี 18
          borderRadius: BorderRadius.circular(18),
          // เงาใต้ปุ่ม
          boxShadow: [
            // เงาหนึ่งชั้น
            BoxShadow(
              // สีเงาม่วงโปร่งใส 35 เปอร์เซ็นต์
              color: AppColors.primary.withOpacity(0.35),
              // ความเบลอของเงา 20
              blurRadius: 20,
              // เงาเลื่อนลง 10 พิกเซล
              offset: const Offset(0, 10),
            ),
          ],
        ),
        // Material โปร่งใสเพื่อให้ InkWell แสดงเอฟเฟกต์ริปเปิลได้
        child: Material(
          // สีโปร่งใสไม่บังพื้นหลัง gradient
          color: Colors.transparent,
          // InkWell ตรวจจับการแตะและวาดริปเปิล
          child: InkWell(
            // ให้ริปเปิลมีขอบมนเท่าปุ่ม
            borderRadius: BorderRadius.circular(18),
            // เมื่อแตะให้เรียก onPressed
            onTap: onPressed,
            // แถวเรียงข้อความและไอคอนตามแนวนอน
            child: Row(
              // จัดให้อยู่กึ่งกลางแนวนอน
              mainAxisAlignment: MainAxisAlignment.center,
              // รายการลูกของแถว
              children: [
                // ข้อความของปุ่ม
                Text(
                  // ข้อความ label
                  label,
                  // ใช้สไตล์ titleMedium ของธีมแล้วปรับแต่ง
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        // ตัวอักษรสีขาว
                        color: Colors.white,
                        // ตัวหนา w600
                        fontWeight: FontWeight.w600,
                      ),
                ),
                // ถ้ามี trailingIcon ให้เพิ่ม widget หลายตัวด้วย spread operator
                if (trailingIcon != null) ...[
                  // ช่องว่างกว้าง 8
                  const SizedBox(width: 8),
                  // ไอคอนสีขาวขนาด 20
                  Icon(trailingIcon, color: Colors.white, size: 20),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
