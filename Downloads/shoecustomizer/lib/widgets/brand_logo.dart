// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้า flutter_svg สำหรับวาดไฟล์ SVG
import 'package:flutter_svg/flutter_svg.dart';
// นำเข้าแพ็กเกจ http เพื่อดาวน์โหลด SVG ผ่านเน็ต ตั้งชื่อย่อ http
import 'package:http/http.dart' as http;

/// แสดงโลโก้แบรนด์จริง (SVG) โดยดึงเองผ่าน http เพื่อควบคุม error handling ได้เต็มที่
/// ถ้าโหลดไม่สำเร็จ (เน็ตหลุด/ลิงก์เสีย) จะ fallback ไปแสดง emoji แทนอัตโนมัติ
/// ไม่ทำให้แอปพังหรือค้าง
// วิดเจ็ตโลโก้แบรนด์ เป็น StatefulWidget เพราะต้องเก็บ Future ที่กำลังโหลด
class BrandLogo extends StatefulWidget {
  // ลิงก์ของไฟล์ SVG
  final String url;
  // สีที่ใช้ย้อมโลโก้
  final Color color;
  // อีโมจิสำรองเมื่อโหลดไม่สำเร็จ
  final String fallbackEmoji;
  // ขนาดกว้างและสูงของโลโก้
  final double size;

  // constructor แบบ const
  const BrandLogo({
    // ส่งต่อ key ให้คลาสแม่
    super.key,
    // url ต้องระบุ
    required this.url,
    // color ต้องระบุ
    required this.color,
    // fallbackEmoji ต้องระบุ
    required this.fallbackEmoji,
    // size ค่าเริ่มต้น 32
    this.size = 32,
  });

  // เขียนทับ createState เพื่อสร้าง State ของวิดเจ็ตนี้
  @override
  // คืนอ็อบเจ็กต์ _BrandLogoState
  State<BrandLogo> createState() => _BrandLogoState();
}

// คลาส State ที่เก็บสถานะการโหลด
class _BrandLogoState extends State<BrandLogo> {
  // ตัวแปรเก็บ Future ของข้อความ SVG (ตั้งค่าใน initState เพียงครั้งเดียว)
  late Future<String?> _svgFuture;

  // เขียนทับ initState (เรียกครั้งเดียวตอนสร้างวิดเจ็ต)
  @override
  // เรียก initState ของคลาสแม่ก่อนเสมอ
  void initState() {
    super.initState();
    // เริ่มดาวน์โหลดทันทีและเก็บ Future ไว้ ป้องกันการโหลดซ้ำทุกครั้งที่ rebuild
    _svgFuture = _fetchSvg();
  }

  // ฟังก์ชันดาวน์โหลด SVG คืนข้อความ SVG หรือ null ถ้าล้มเหลว
  Future<String?> _fetchSvg() async {
    // ใช้ try เพื่อดักข้อผิดพลาดเครือข่าย
    try {
      // ส่ง GET ไปยัง url และตั้งเวลาหมดอายุ 6 วินาที
      final response = await http.get(Uri.parse(widget.url)).timeout(const Duration(seconds: 6));
      // ถ้าสถานะ 200 (สำเร็จ) คืนเนื้อหา SVG
      if (response.statusCode == 200) return response.body;
    // จับข้อผิดพลาดทุกชนิด (เน็ตหลุด ลิงก์เสีย timeout)
    } catch (_) {
      // เน็ตหลุด/ลิงก์เสีย/timeout — ปล่อยให้ fallback เป็น emoji
    }
    // ถึงบรรทัดนี้แปลว่าล้มเหลว คืน null เพื่อให้ไปแสดง emoji
    return null;
  }

  // เขียนทับ build
  @override
  // build คืนหน้าตาโลโก้
  Widget build(BuildContext context) {
    // FutureBuilder สร้าง UI ตามสถานะของ Future
    return FutureBuilder<String?>(
      // Future ที่ต้องรอ
      future: _svgFuture,
      // ฟังก์ชันสร้าง UI ตามสถานะ snapshot
      builder: (context, snapshot) {
        // ถ้ายังโหลดไม่เสร็จ
        if (snapshot.connectionState != ConnectionState.done) {
          // แสดงกล่องขนาดเท่าโลโก้
          return SizedBox(
            // กว้างเท่า size
            width: widget.size,
            // สูงเท่า size
            height: widget.size,
            // จัดให้อยู่กึ่งกลาง
            child: Center(
              // กล่องของตัวหมุน
              child: SizedBox(
                // กว้างครึ่งหนึ่งของ size
                width: widget.size * 0.5,
                // สูงครึ่งหนึ่งของ size
                height: widget.size * 0.5,
                // ตัวหมุนโหลดบาง ๆ สีโปร่งใส 40 เปอร์เซ็นต์
                child: CircularProgressIndicator(strokeWidth: 2, color: widget.color.withOpacity(0.4)),
              ),
            ),
          );
        }

        // ดึงข้อความ SVG ที่โหลดมา
        final svgString = snapshot.data;
        // ถ้าเป็น null คือโหลดไม่สำเร็จ
        if (svgString == null) {
          // แสดงกล่องขนาดเท่าโลโก้
          return SizedBox(
            // กว้างเท่า size
            width: widget.size,
            // สูงเท่า size
            height: widget.size,
            // จัดกึ่งกลาง
            child: Center(
              // แสดงอีโมจิสำรอง ขนาดฟอนต์ 65 เปอร์เซ็นต์ของ size
              child: Text(widget.fallbackEmoji, style: TextStyle(fontSize: widget.size * 0.65)),
            ),
          );
        }

        // โหลดสำเร็จ แสดง SVG จากข้อความ
        return SvgPicture.string(
          // ข้อความ SVG
          svgString,
          // กว้าง
          width: widget.size,
          // สูง
          height: widget.size,
          // ย้อม SVG ให้เป็นสีเดียวด้วย BlendMode.srcIn
          colorFilter: ColorFilter.mode(widget.color, BlendMode.srcIn),
        );
      },
    );
  }
}
