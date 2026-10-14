// นำเข้าไลบรารี Material ของ Flutter (Widget พื้นฐานทั้งหมด เช่น MaterialApp, Scaffold)
import 'package:flutter/material.dart';
// นำเข้า provider สำหรับจัดการ state กลางของแอป
import 'package:provider/provider.dart';
// นำเข้า CustomizerState ซึ่งเก็บสถานะการออกแบบรองเท้า
import 'state/customizer_state.dart';
// นำเข้าธีม (สี/ฟอนต์) ของแอป
import 'theme/app_theme.dart';
// นำเข้าหน้า Splash (หน้าแรกที่แสดงตอนเปิดแอป)
import 'screens/splash_screen.dart';
// นำเข้าหน้าเลือกแบรนด์
import 'screens/brand_selection_screen.dart';
// นำเข้าหน้าเลือกรุ่นรองเท้า
import 'screens/model_selection_screen.dart';
// นำเข้าหน้าตกแต่งรองเท้า (หน้าหลักของแอป)
import 'screens/customizer_screen.dart';
// นำเข้าหน้าสรุปออเดอร์
import 'screens/summary_screen.dart';

// จุดเริ่มต้นของโปรแกรม Dart ทุกครั้งที่เปิดแอปจะเริ่มทำงานที่ฟังก์ชันนี้
void main() {
  // สั่งให้ Flutter วาดวิดเจ็ต ShoeCustomizerApp เป็นรากของแอป
  runApp(const ShoeCustomizerApp());
}

/// โครงสร้าง Route ทั้งหมดของแอป:
/// / (splash) -> /brands -> /models (รับ brandId) -> /customizer (รับ modelId) -> /summary
///
/// CustomizerState ถูกสร้างไว้ที่ระดับบนสุด (ChangeNotifierProvider ครอบ MaterialApp)
/// เพื่อให้หน้า customizer และ summary ใช้ state ร่วมกันได้ผ่าน provider
// วิดเจ็ตรากของแอป เป็น StatelessWidget เพราะตัวมันเองไม่มีสถานะที่เปลี่ยนแปลง (state อยู่ใน CustomizerState)
class ShoeCustomizerApp extends StatelessWidget {
  // constructor แบบ const รับ key ส่งต่อให้ class แม่
  const ShoeCustomizerApp({super.key});

  // บอกว่าเรากำลังเขียนทับเมธอด build ของ class แม่
  @override
  // build คือเมธอดที่ Flutter เรียกเพื่อสร้างหน้าตา UI
  Widget build(BuildContext context) {
    // ครอบแอปด้วย ChangeNotifierProvider เพื่อให้ทุกหน้าเข้าถึง CustomizerState ตัวเดียวกันได้
    return ChangeNotifierProvider(
      // สร้างอ็อบเจ็กต์ CustomizerState หนึ่งตัวตอนเริ่มแอป (ตัวแปร _ คือ context ที่ไม่ได้ใช้)
      create: (_) => CustomizerState(),
      // MaterialApp ตัวจัดการ Navigator, ธีม และ route ของทั้งแอป
      child: MaterialApp(
        // ชื่อแอปที่ระบบปฏิบัติการใช้แสดง เช่น ในหน้า recent apps
        title: 'Shoe Studio',
        // ปิดป้าย DEBUG มุมขวาบนตอนรันโหมด debug
        debugShowCheckedModeBanner: false,
        // ใช้ธีมที่ประกอบจากฟังก์ชัน buildAppTheme() ใน app_theme.dart
        theme: buildAppTheme(),
        // หน้าแรกที่เปิดคือ route '/' (SplashScreen)
        initialRoute: '/',
        // ตาราง route แบบตั้งชื่อ (named routes) ใช้กับ Navigator.pushNamed
        routes: {
          // route '/' แสดงหน้า Splash
          '/': (context) => const SplashScreen(),
          // route '/brands' แสดงหน้าเลือกแบรนด์
          '/brands': (context) => const BrandSelectionScreen(),
          // route '/models' แสดงหน้าเลือกรุ่น (รับ brandId ผ่าน arguments)
          '/models': (context) => const ModelSelectionScreen(),
          // route '/customizer' แสดงหน้าตกแต่งรองเท้า
          '/customizer': (context) => const CustomizerScreen(),
          // route '/summary' แสดงหน้าสรุปออเดอร์
          '/summary': (context) => const SummaryScreen(),
        },
      ),
    );
  }
}
