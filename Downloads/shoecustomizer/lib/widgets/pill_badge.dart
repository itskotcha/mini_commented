// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าธีมเพื่อใช้ AppColors
import '../theme/app_theme.dart';

/// ป้ายทรงพิลพื้นหลังม่วงอ่อน ใช้แสดงราคา/สถานะสั้น ๆ
// ป้ายทรงแคปซูลแสดงข้อความสั้น เป็น StatelessWidget
class PillBadge extends StatelessWidget {
  // ข้อความในป้าย
  final String text;
  // สีพื้นหลัง (ไม่ระบุจะใช้ม่วงอ่อน)
  final Color? background;
  // สีตัวอักษร (ไม่ระบุจะใช้ม่วงหลัก)
  final Color? textColor;
  // ไอคอนหน้าข้อความ (ไม่บังคับ)
  final IconData? icon;

  // constructor แบบ const
  const PillBadge({
    // ส่งต่อ key
    super.key,
    // text ต้องระบุ
    required this.text,
    // background ไม่บังคับ
    this.background,
    // textColor ไม่บังคับ
    this.textColor,
    // icon ไม่บังคับ
    this.icon,
  });

  // เขียนทับ build
  @override
  // build คืนหน้าตาป้าย
  Widget build(BuildContext context) {
    // Container ทำกรอบพร้อมสีพื้นหลัง
    return Container(
      // ระยะห่างภายใน ซ้ายขวา 12 บนล่าง 6
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      // ตกแต่งกรอบ
      decoration: BoxDecoration(
        // สีพื้นหลัง ถ้า background เป็น null ใช้ primarySoft
        color: background ?? AppColors.primarySoft,
        // ขอบมน 999 ทำให้เป็นแคปซูลเต็มวง
        borderRadius: BorderRadius.circular(999),
      ),
      // แถวเรียงไอคอนกับข้อความ
      child: Row(
        // ให้แถวกว้างเท่าเนื้อหา ไม่ขยายเต็ม
        mainAxisSize: MainAxisSize.min,
        // รายการลูก
        children: [
          // ถ้ามีไอคอนให้แสดงไอคอนและช่องว่างต่อท้าย
          if (icon != null) ...[
            // ไอคอนขนาด 14 สีตามที่กำหนดหรือม่วงหลัก
            Icon(icon, size: 14, color: textColor ?? AppColors.primary),
            // ช่องว่างกว้าง 4
            const SizedBox(width: 4),
          ],
          // ข้อความในป้าย
          Text(
            // ตัวอักษร text
            text,
            // ใช้สไตล์ labelMedium ของธีม
            style: Theme.of(context).textTheme.labelMedium?.copyWith(
                  // สีตัวอักษรตามที่กำหนดหรือม่วงหลัก
                  color: textColor ?? AppColors.primary,
                  // ตัวหนา w600
                  fontWeight: FontWeight.w600,
                ),
          ),
        ],
      ),
    );
  }
}
