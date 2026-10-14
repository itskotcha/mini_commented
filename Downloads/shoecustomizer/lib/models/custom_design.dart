// นำเข้า dart:typed_data เพื่อใช้ Uint8List เก็บไบต์ของรูปภาพ
import 'dart:typed_data';

// นำเข้า Material ของ Flutter (ใช้ชนิด Color)
import 'package:flutter/material.dart';

/// โหมดเครื่องมือในหน้าตกแต่งรองเท้า
// enum ของเครื่องมือในหน้าตกแต่ง: เปลี่ยนสี, สติกเกอร์, วาด
enum CustomizerTool { color, sticker, draw }

/// รูปแบบหัวปากกาที่รองรับ
// enum ของชนิดหัวปากกา: หัวกลม, มาร์กเกอร์, ไฮไลต์, สเปรย์
enum BrushType { round, marker, highlighter, spray }

/// รูปภาพที่ผู้ใช้นำเข้ามาเป็นสติกเกอร์
// คลาสแทนสติกเกอร์หนึ่งชิ้นที่ผู้ใช้วางบนรองเท้า
class ShoeSticker {
  // รหัสเฉพาะของสติกเกอร์
  final String id;
  // ไบต์ของไฟล์รูปที่ผู้ใช้เลือก
  final Uint8List imageBytes;
  /// ตำแหน่ง UV และ material บนผิว mesh เช่น uv|3|0.42|0.67
  /// ค่านี้ผูกกับ texture ของโมเดล ไม่ใช่พิกัด 2D บนหน้าจอ
  // ตำแหน่งบนผิวโมเดลในรูปแบบข้อความ uv แล้วตามด้วยเลข material และพิกัด u,v (ผูกกับ texture ไม่ใช่พิกัดหน้าจอ)
  final String surface;
  // อัตราขยาย (1 คือขนาดเดิม)
  final double scale;
  // มุมหมุน (หน่วยองศา)
  final double rotation;

  // constructor แบบ const
  const ShoeSticker({
    // id ต้องระบุ
    required this.id,
    // imageBytes ต้องระบุ
    required this.imageBytes,
    // surface ต้องระบุ
    required this.surface,
    // scale ค่าเริ่มต้น 1
    this.scale = 1,
    // rotation ค่าเริ่มต้น 0
    this.rotation = 0,
  });

  // เมธอด copyWith สร้างสำเนาใหม่โดยเปลี่ยนเฉพาะฟิลด์ที่ส่งมา (เพราะคลาสนี้ immutable)
  ShoeSticker copyWith({
    // พารามิเตอร์ id แบบ nullable
    String? id,
    // พารามิเตอร์ imageBytes แบบ nullable
    Uint8List? imageBytes,
    // พารามิเตอร์ surface แบบ nullable
    String? surface,
    // พารามิเตอร์ scale แบบ nullable
    double? scale,
    // พารามิเตอร์ rotation แบบ nullable
    double? rotation,
  }) {
    // สร้างอ็อบเจ็กต์ใหม่
    return ShoeSticker(
      // ถ้าไม่ส่ง id มาใช้ค่าเดิม (ตัวดำเนินการ ??)
      id: id ?? this.id,
      // ใช้ imageBytes เดิมถ้าไม่ได้ส่งมา
      imageBytes: imageBytes ?? this.imageBytes,
      // ใช้ surface เดิมถ้าไม่ได้ส่งมา
      surface: surface ?? this.surface,
      // ใช้ scale เดิมถ้าไม่ได้ส่งมา
      scale: scale ?? this.scale,
      // ใช้ rotation เดิมถ้าไม่ได้ส่งมา
      rotation: rotation ?? this.rotation,
    );
  }
}

/// เส้นหนึ่งเส้นที่วาดบนผิวรองเท้า 3D
/// แต่ละจุดเป็นตำแหน่ง UV บน material จึงแนบและหมุนตามผิว mesh จริง
// คลาสแทนเส้นที่วาดหนึ่งเส้น (ลากนิ้วหนึ่งครั้ง)
class DrawingStroke {
  // รหัสเฉพาะของเส้น
  final String id;
  // รายการจุดของเส้น แต่ละจุดเป็นข้อความตำแหน่ง UV
  final List<String> surfacePoints;
  // สีของเส้น
  final Color color;
  // ความหนาของเส้น
  final double width;
  // ชนิดหัวปากกา
  final BrushType brushType;

  // constructor (ไม่ใช่ const เพราะ surfacePoints เป็นลิสต์ที่ต่อเพิ่มได้)
  DrawingStroke({
    // id ต้องระบุ
    required this.id,
    // surfacePoints ต้องระบุ
    required this.surfacePoints,
    // color ต้องระบุ
    required this.color,
    // width ต้องระบุ
    required this.width,
    // brushType ต้องระบุ
    required this.brushType,
  });
}
