// นำเข้า Material ของ Flutter
import 'package:flutter/material.dart';
// นำเข้า google_fonts เพื่อใช้ฟอนต์ Poppins จาก Google Fonts
import 'package:google_fonts/google_fonts.dart';

/// ==========================================
///  DESIGN TOKENS — โทนสีม่วงไล่เฉดพรีเมียม
/// ==========================================

// คลาสเก็บค่าสีมาตรฐานของแอป (Design Tokens) เพื่อใช้สีเดียวกันทุกหน้า
class AppColors {
  // constructor แบบ private ป้องกันการสร้างอ็อบเจ็กต์ เพราะคลาสนี้มีไว้เก็บค่า static เท่านั้น
  AppColors._();

  // สีม่วงหลักของแบรนด์แอป
  static const Color primary = Color(0xFF7C5CFC); // ม่วงหลัก
  // สีม่วงเข้ม ใช้เป็นปลายของ gradient
  static const Color primaryDark = Color(0xFF5B3DEB); // ม่วงเข้ม (ใช้ทำ gradient)
  // สีม่วงอ่อนมาก ใช้เป็นพื้นหลังชิป/ป้าย
  static const Color primarySoft = Color(0xFFEFE9FF); // ม่วงอ่อนมาก (พื้นหลังชิป/แท็ก)
  // สีพื้นหลังโดยรวมของทุกหน้า
  static const Color background = Color(0xFFF7F5FF); // พื้นหลังแอปโดยรวม
  // สีพื้นผิวของการ์ด (ขาว)
  static const Color surface = Colors.white;
  // สีตัวอักษรหลัก (ม่วงเข้มเกือบดำ)
  static const Color textDark = Color(0xFF241C3D);
  // สีตัวอักษรรอง/คำอธิบาย (เทาอมม่วง)
  static const Color textGray = Color(0xFF8B85A6);
  // สีชมพูเน้น (accent)
  static const Color accentPink = Color(0xFFFF7AC6);
  // สีเขียวแสดงความสำเร็จ
  static const Color success = Color(0xFF34C77B);

  // gradient หลักใช้กับปุ่มและองค์ประกอบเด่น
  static const LinearGradient primaryGradient = LinearGradient(
    // เริ่มไล่สีจากมุมซ้ายบน
    begin: Alignment.topLeft,
    // ไปสิ้นสุดที่มุมขวาล่าง
    end: Alignment.bottomRight,
    // สีเริ่มต้น (ม่วงสว่าง) ไปสีปลาย primaryDark
    colors: [Color(0xFF9A7BFF), primaryDark],
  );

  // gradient สำหรับพื้นหลังหน้า Splash (hero)
  static const LinearGradient heroGradient = LinearGradient(
    // ไล่สีจากบนลงล่าง
    begin: Alignment.topCenter,
    // สิ้นสุดที่กึ่งกลางด้านล่าง
    end: Alignment.bottomCenter,
    // ม่วงสว่างด้านบน ม่วงเข้มด้านล่าง
    colors: [Color(0xFF8B6EFC), Color(0xFF5B3DEB)],
  );
}

/// ==========================================
///  THEME DATA
/// ==========================================

// ฟังก์ชันประกอบ ThemeData ทั้งแอป เรียกใช้ใน main.dart
ThemeData buildAppTheme() {
  // สร้างธีมฐานก่อนแล้วค่อยปรับแต่งต่อด้วย copyWith
  final base = ThemeData(
    // เปิดใช้ Material 3
    useMaterial3: true,
    // สร้างชุดสีทั้งหมดจากสีเมล็ด (seed) อัตโนมัติ
    colorScheme: ColorScheme.fromSeed(
      // ใช้สีม่วงหลักเป็นสีเมล็ด
      seedColor: AppColors.primary,
      // ธีมโทนสว่าง
      brightness: Brightness.light,
      // บังคับให้ primary เป็นสีม่วงของแอปแทนค่าที่คำนวณเอง
      primary: AppColors.primary,
      // บังคับสีพื้นผิวเป็นสีขาว
      surface: AppColors.surface,
    ),
  );

  // นำ textTheme ของ Material มาเปลี่ยนฟอนต์เป็น Poppins
  final textTheme = GoogleFonts.poppinsTextTheme(base.textTheme).apply(
    // กำหนดสีตัวอักษรเนื้อหาเป็น textDark
    bodyColor: AppColors.textDark,
    // กำหนดสีตัวอักษรหัวข้อใหญ่เป็น textDark
    displayColor: AppColors.textDark,
  );

  // คืนธีมฐานที่ถูกแก้ไขบางส่วน
  return base.copyWith(
    // สีพื้นหลังของ Scaffold ทุกหน้า
    scaffoldBackgroundColor: AppColors.background,
    // ใช้ textTheme ที่เปลี่ยนฟอนต์แล้ว
    textTheme: textTheme,
    // ปรับสไตล์ AppBar
    appBarTheme: AppBarTheme(
      // พื้นหลังโปร่งใส
      backgroundColor: Colors.transparent,
      // ปิดสีเคลือบ surface tint ของ Material 3
      surfaceTintColor: Colors.transparent,
      // ไม่มีเงา
      elevation: 0,
      // สีไอคอนและตัวอักษรบน AppBar
      foregroundColor: AppColors.textDark,
      // ชื่อหน้าชิดซ้าย (ไม่อยู่กลาง)
      centerTitle: false,
      // สไตล์ตัวอักษรชื่อหน้า
      titleTextStyle: GoogleFonts.poppins(
        // ขนาด 20
        fontSize: 20,
        // น้ำหนักตัวหนา w600
        fontWeight: FontWeight.w600,
        // สีตัวอักษร
        color: AppColors.textDark,
      ),
      // สีของไอคอนบน AppBar
      iconTheme: const IconThemeData(color: AppColors.textDark),
    ),
    // ธีมของปุ่ม FilledButton
    filledButtonTheme: FilledButtonThemeData(
      // กำหนดสไตล์ปุ่ม
      style: FilledButton.styleFrom(
        // พื้นหลังปุ่มสีม่วง
        backgroundColor: AppColors.primary,
        // ตัวอักษรบนปุ่มสีขาว
        foregroundColor: Colors.white,
        // ระยะห่างบนล่างภายในปุ่ม 16
        padding: const EdgeInsets.symmetric(vertical: 16),
        // ขอบมนรัศมี 18
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
        // ฟอนต์ตัวอักษรบนปุ่ม
        textStyle: GoogleFonts.poppins(fontWeight: FontWeight.w600, fontSize: 16),
      ),
    ),
    // ธีมของ Card
    cardTheme: CardThemeData(
      // ไม่มีเงา
      elevation: 0,
      // สีการ์ดขาว
      color: AppColors.surface,
      // ปิด surface tint
      surfaceTintColor: Colors.transparent,
      // ขอบมนรัศมี 22
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(22)),
    ),
    // ธีมของ Chip (ป้ายตัวเลือก)
    chipTheme: base.chipTheme.copyWith(
      // พื้นหลัง chip ปกติ
      backgroundColor: AppColors.surface,
      // สีพื้นหลังเมื่อถูกเลือก (ม่วง)
      selectedColor: AppColors.primary,
      // สไตล์ตัวอักษรเมื่อไม่ถูกเลือก
      labelStyle: GoogleFonts.poppins(fontWeight: FontWeight.w500, color: AppColors.textDark),
      // สไตล์ตัวอักษรเมื่อถูกเลือก (ขาว)
      secondaryLabelStyle: GoogleFonts.poppins(fontWeight: FontWeight.w600, color: Colors.white),
      // รูปทรง Stadium (แคปซูล) ขอบม่วงโปร่งใส 25 เปอร์เซ็นต์
      shape: StadiumBorder(side: BorderSide(color: AppColors.primary.withOpacity(0.25))),
      // ระยะห่างภายใน chip
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
    ),
    // ธีมเส้นคั่น สีเทาจาง ๆ
    dividerTheme: DividerThemeData(color: AppColors.textGray.withOpacity(0.15)),
    // ปรับ colorScheme เพิ่มเติม
    colorScheme: base.colorScheme.copyWith(
      // สี surfaceVariant เป็นม่วงอ่อน
      surfaceVariant: AppColors.primarySoft,
      // สีตัวอักษรบน surfaceVariant เป็นเทา
      onSurfaceVariant: AppColors.textGray,
    ),
  );
}
