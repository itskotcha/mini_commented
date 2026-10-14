// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าธีมเพื่อใช้ AppColors
import '../theme/app_theme.dart';

/// หน้า 1: Splash Screen
/// พื้นหลังไล่เฉดสีม่วง โลโก้วงกลมสีขาวตรงกลาง แอนิเมชัน fade+scale เบา ๆ
/// แสดง 2 วินาที แล้ว Navigate ไปหน้า brands พร้อม clear backstack
// หน้า Splash เป็น StatefulWidget เพราะต้องมี AnimationController
class SplashScreen extends StatefulWidget {
  // constructor แบบ const
  const SplashScreen({super.key});

  @override
  // สร้าง State ของหน้านี้
  State<SplashScreen> createState() => _SplashScreenState();
}

// State ที่ผสม SingleTickerProviderStateMixin เพื่อให้ AnimationController ใช้ vsync ได้
class _SplashScreenState extends State<SplashScreen> with SingleTickerProviderStateMixin {
  // ตัวควบคุมแอนิเมชัน
  late final AnimationController _controller;
  // แอนิเมชันค่าความโปร่งใส 0 ถึง 1
  late final Animation<double> _fade;
  // แอนิเมชันค่าขนาด 0.85 ถึง 1.0
  late final Animation<double> _scale;

  @override
  // เริ่มต้นหน้าจอ
  void initState() {
    // เรียก initState ของคลาสแม่
    super.initState();
    // สร้าง controller ความยาว 700 มิลลิวินาที โดยผูกกับ State นี้เป็น vsync
    _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 700));
    // fade ใช้ curve easeOut (เริ่มเร็วแล้วช้าลง)
    _fade = CurvedAnimation(parent: _controller, curve: Curves.easeOut);
    // scale ไล่จาก 0.85 ถึง 1.0
    _scale = Tween<double>(begin: 0.85, end: 1.0)
        // ใช้ curve easeOutBack (เกินเล็กน้อยแล้วเด้งกลับ)
        .animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutBack));
    // เริ่มเล่นแอนิเมชัน
    _controller.forward();

    // หน่วงเวลา 2 วินาทีแล้วเปลี่ยนหน้า
    Future.delayed(const Duration(seconds: 2), () {
      // ถ้าหน้านี้ถูกถอดไปแล้ว (ไม่ mounted) ให้เลิกทำ ป้องกัน error
      if (!mounted) return;
      // ไปหน้า /brands และล้าง backstack ทั้งหมด เพื่อไม่ให้กดย้อนกลับมา splash ได้
      Navigator.of(context).pushNamedAndRemoveUntil('/brands', (route) => false);
    });
  }

  // เขียนทับ dispose
  @override
  // คืนทรัพยากรของ controller ป้องกัน memory leak
  void dispose() {
    // เรียก dispose ของคลาสแม่
    _controller.dispose();
    super.dispose();
  }

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // Scaffold โครงหน้าพื้นฐาน
    return Scaffold(
      // พื้นหลังด้วย DecoratedBox
      body: DecoratedBox(
        // ใช้ heroGradient ไล่สีม่วง
        decoration: const BoxDecoration(gradient: AppColors.heroGradient),
        // จัดเนื้อหาให้อยู่กึ่งกลางหน้าจอ
        child: Center(
          // เอฟเฟกต์ค่อย ๆ ปรากฏ
          child: FadeTransition(
            // ผูกกับ _fade
            opacity: _fade,
            // เอฟเฟกต์ค่อย ๆ ขยาย
            child: ScaleTransition(
              // ผูกกับ _scale
              scale: _scale,
              // คอลัมน์เรียงจากบนลงล่าง
              child: Column(
                // ให้คอลัมน์สูงเท่าเนื้อหา
                mainAxisSize: MainAxisSize.min,
                // รายการลูก
                children: [
                  // กล่องโลโก้สีขาว
                  Container(
                    // กว้าง 108
                    width: 108,
                    // สูง 108
                    height: 108,
                    // ตกแต่งกล่อง
                    decoration: BoxDecoration(
                      // สีขาว
                      color: Colors.white,
                      // ขอบมน 32
                      borderRadius: BorderRadius.circular(32),
                      // เงา
                      boxShadow: [
                        // เงาหนึ่งชั้น
                        BoxShadow(
                          // สีดำโปร่งใส 15 เปอร์เซ็นต์
                          color: Colors.black.withOpacity(0.15),
                          // เบลอ 30
                          blurRadius: 30,
                          // เลื่อนลง 12
                          offset: const Offset(0, 12),
                        ),
                      ],
                    ),
                    // จัดเนื้อหาในกล่องให้อยู่กึ่งกลาง
                    alignment: Alignment.center,
                    // แสดงอีโมจิรองเท้าขนาด 52
                    child: const Text('👟', style: TextStyle(fontSize: 52)),
                  ),
                  // เว้นช่อง 28
                  const SizedBox(height: 28),
                  // ข้อความชื่อแอป
                  Text(
                    // ข้อความ SHOE STUDIO
                    'SHOE STUDIO',
                    // ใช้สไตล์ headlineMedium
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                          // สีขาว
                          color: Colors.white,
                          // ตัวหนา w700
                          fontWeight: FontWeight.w700,
                          // เว้นระยะตัวอักษร 1.5
                          letterSpacing: 1.5,
                        ),
                  ),
                  // เว้นช่อง 10
                  const SizedBox(height: 10),
                  // ข้อความคำโปรย
                  Text(
                    // ข้อความ ออกแบบรองเท้าในสไตล์ของคุณ
                    'ออกแบบรองเท้าในสไตล์ของคุณ',
                    // ใช้สไตล์ bodyMedium
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          // สีขาวโปร่งใส 85 เปอร์เซ็นต์
                          color: Colors.white.withOpacity(0.85),
                        ),
                  ),
                  // เว้นช่อง 40
                  const SizedBox(height: 40),
                  // กล่องกำหนดขนาดตัวหมุนโหลด
                  SizedBox(
                    // กว้าง 32
                    width: 32,
                    // สูง 32
                    height: 32,
                    // ตัวหมุนโหลด
                    child: CircularProgressIndicator(
                      // ความหนาเส้น 2.5
                      strokeWidth: 2.5,
                      // สีของเส้นสีขาวโปร่งใส 85 เปอร์เซ็นต์
                      valueColor: AlwaysStoppedAnimation(Colors.white.withOpacity(0.85)),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
