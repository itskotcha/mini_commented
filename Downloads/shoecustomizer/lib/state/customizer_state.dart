// นำเข้า Uint8List สำหรับไบต์รูปสติกเกอร์
import 'dart:typed_data';

// นำเข้า Material (ใช้ Color และ ChangeNotifier)
import 'package:flutter/material.dart';
// นำเข้าโมเดลสติกเกอร์/เส้นวาด/enum
import '../models/custom_design.dart';
// นำเข้ารายการชิ้นส่วนรองเท้า
import '../models/shoe_parts.dart';

/// State กลางของแอป ใช้เก็บสถานะการออกแบบรองเท้า
/// - รุ่นที่กำลังออกแบบอยู่ (selectedModelId)
/// - ชิ้นส่วนที่กำลังถูกเลือกอยู่บนหน้าจอ (selectedPartId) — อ้างอิงจาก shoeParts
/// - สีของแต่ละชิ้นส่วน (partColors: partId -> Color)
///
/// ใช้ ChangeNotifier + provider package เพื่อให้หน้า Customizer
/// และหน้า Summary อ่าน/แก้ไขค่าเดียวกันได้ และ UI rebuild อัตโนมัติ
// ChangeNotifier คือคลาสที่เรียก notifyListeners() เพื่อบอก UI ที่ฟังอยู่ให้วาดใหม่
class CustomizerState extends ChangeNotifier {
  // รหัสรุ่นรองเท้าที่เลือก (null ถ้ายังไม่เลือก)
  String? selectedModelId;

  // ชิ้นส่วนที่กำลังเลือกอยู่ เริ่มต้นเป็นชิ้นแรกของรายการ
  String selectedPartId = shoeParts.first.id;

  // key = partId, value = สีปัจจุบัน — เริ่มต้นทุกชิ้นเป็นสีขาว
  // แผนที่ partId ไปยังสีปัจจุบัน
  final Map<String, Color> partColors = {
    // สร้างแผนที่ด้วย collection for ให้ทุกชิ้นส่วนเริ่มเป็นสีขาว
    for (final part in shoeParts) part.id: Colors.white,
  };

  // รายการสติกเกอร์ทั้งหมดบนรองเท้า
  final List<ShoeSticker> stickers = [];
  // รายการเส้นที่วาดทั้งหมด
  final List<DrawingStroke> drawingStrokes = [];

  // สีปากกาปัจจุบัน (เริ่มต้นดำ)
  Color brushColor = Colors.black;
  // ขนาดปากกาปัจจุบัน
  double brushSize = 7;
  // ชนิดหัวปากกาปัจจุบัน
  BrushType brushType = BrushType.round;

  // เซ็ตรุ่นรองเท้าที่เลือก
  void setModel(String modelId) {
    // เก็บค่ารหัสรุ่น
    selectedModelId = modelId;
    // แจ้ง UI ให้วาดใหม่
    notifyListeners();
  }

  // เปลี่ยนชิ้นส่วนที่เลือกอยู่
  void selectPart(String partId) {
    // เก็บ id ชิ้นส่วนใหม่
    selectedPartId = partId;
    // แจ้ง UI
    notifyListeners();
  }

  // เปลี่ยนสีของชิ้นส่วนที่เลือกอยู่
  void selectColorForCurrentPart(Color color) {
    // เขียนสีใหม่ลงแผนที่
    partColors[selectedPartId] = color;
    // แจ้ง UI
    notifyListeners();
  }

  // ดึงสีของชิ้นส่วน ถ้าไม่มีให้คืนสีขาว
  Color colorOf(String partId) => partColors[partId] ?? Colors.white;

  // เพิ่มสติกเกอร์ใหม่
  void addSticker({
    // พารามิเตอร์ id
    required String id,
    // พารามิเตอร์ imageBytes
    required Uint8List imageBytes,
    // พารามิเตอร์ surface
    required String surface,
  }) {
    // ถ้ามีสติกเกอร์ id นี้อยู่แล้วให้ข้าม (กันเพิ่มซ้ำ)
    if (stickers.any((sticker) => sticker.id == id)) return;
    // เพิ่มเข้าลิสต์
    stickers.add(
      // สร้างอ็อบเจ็กต์ ShoeSticker
      ShoeSticker(
        // ใส่ id
        id: id,
        // ใส่ไบต์รูป
        imageBytes: imageBytes,
        // ใส่ตำแหน่งผิว
        surface: surface,
      ),
    );
    // แจ้ง UI
    notifyListeners();
  }

  // อัปเดตสติกเกอร์ (ย้าย/ย่อขยาย/หมุน) ส่งเฉพาะค่าที่ต้องการเปลี่ยน
  void updateSticker(
    // id ของสติกเกอร์ที่จะแก้
    String id, {
    // ตำแหน่งใหม่ (ถ้ามี)
    String? surface,
    // ขนาดใหม่ (ถ้ามี)
    double? scale,
    // มุมใหม่ (ถ้ามี)
    double? rotation,
  }) {
    // หาลำดับของสติกเกอร์ในลิสต์
    final index = stickers.indexWhere((sticker) => sticker.id == id);
    // ไม่พบ (-1) ก็เลิกทำ
    if (index == -1) return;
    // แทนที่ตัวเดิมด้วยสำเนาที่ถูกแก้
    stickers[index] = stickers[index].copyWith(
      // ค่า surface ใหม่
      surface: surface,
      // ค่า scale ใหม่
      scale: scale,
      // ค่า rotation ใหม่
      rotation: rotation,
    );
    // แจ้ง UI
    notifyListeners();
  }

  // ลบสติกเกอร์ตาม id
  void removeSticker(String id) {
    // ลบทุกตัวที่ id ตรงกัน
    stickers.removeWhere((sticker) => sticker.id == id);
    // แจ้ง UI
    notifyListeners();
  }

  // ตั้งสีปากกา
  void setBrushColor(Color color) {
    // เก็บสี
    brushColor = color;
    // แจ้ง UI
    notifyListeners();
  }

  // ตั้งขนาดปากกา
  void setBrushSize(double size) {
    // เก็บขนาด
    brushSize = size;
    // แจ้ง UI
    notifyListeners();
  }

  // ตั้งชนิดหัวปากกา
  void setBrushType(BrushType type) {
    // เก็บชนิด
    brushType = type;
    // แจ้ง UI
    notifyListeners();
  }

  // เริ่มเส้นใหม่เมื่อนิ้วแตะลงบนผิวรองเท้า
  void startSurfaceStroke(String strokeId, String surface) {
    // ถ้ามีเส้น id นี้อยู่แล้วให้ข้าม
    if (drawingStrokes.any((stroke) => stroke.id == strokeId)) return;
    // เพิ่มเส้นใหม่ลงลิสต์
    drawingStrokes.add(
      // สร้าง DrawingStroke
      DrawingStroke(
        // ใส่ id
        id: strokeId,
        // จุดแรกคือตำแหน่งที่แตะ
        surfacePoints: [surface],
        // ใช้สีปากกาปัจจุบัน
        color: brushColor,
        // ใช้ขนาดปากกาปัจจุบัน
        width: brushSize,
        // ใช้ชนิดหัวปากกาปัจจุบัน
        brushType: brushType,
      ),
    );
    // แจ้ง UI
    notifyListeners();
  }

  // เพิ่มจุดให้เส้นที่กำลังลากอยู่
  void appendSurfaceStrokePoint(String strokeId, String surface) {
    // หาลำดับของเส้น
    final index = drawingStrokes.indexWhere((stroke) => stroke.id == strokeId);
    // ไม่พบก็เลิกทำ
    if (index == -1) return;
    // ต่อจุดใหม่ท้ายเส้น (ไม่เรียก notifyListeners เพื่อลดภาระ เพราะตัววาดอยู่ใน WebView อยู่แล้ว)
    drawingStrokes[index].surfacePoints.add(surface);
  }

  // ย้อนกลับเส้นล่าสุด คืน id เส้นที่ถูกลบ
  String? undoLastStroke() {
    // ถ้าไม่มีเส้นเลยคืน null
    if (drawingStrokes.isEmpty) return null;
    // ลบเส้นสุดท้ายและเก็บ id
    final id = drawingStrokes.removeLast().id;
    // แจ้ง UI
    notifyListeners();
    // คืน id เพื่อให้ผู้เรียกสั่งลบใน WebView
    return id;
  }

  // ล้างลายวาดทั้งหมด
  void clearDrawing() {
    // เคลียร์ลิสต์เส้น
    drawingStrokes.clear();
    // แจ้ง UI
    notifyListeners();
  }

  // getter บอกว่ามีการตกแต่ง (สติกเกอร์หรือเส้นวาด) หรือไม่
  bool get hasDecorations => stickers.isNotEmpty || drawingStrokes.isNotEmpty;

  // รีเซ็ตค่าทั้งหมด (ใช้ตอนกลับไปเริ่มออเดอร์ใหม่หลังชำระเงินสำเร็จ)
  // รีเซ็ตทั้งหมดเมื่อเริ่มออเดอร์ใหม่
  void resetDesign() {
    // วนทุกชิ้นส่วน
    for (final part in shoeParts) {
      // ตั้งสีกลับเป็นขาว
      partColors[part.id] = Colors.white;
    }
    // เลือกชิ้นส่วนแรกใหม่
    selectedPartId = shoeParts.first.id;
    // ล้างรุ่นที่เลือก
    selectedModelId = null;
    // ล้างสติกเกอร์
    stickers.clear();
    // ล้างเส้นวาด
    drawingStrokes.clear();
    // สีปากกากลับเป็นดำ
    brushColor = Colors.black;
    // ขนาดปากกากลับเป็น 7
    brushSize = 7;
    // หัวปากกากลับเป็นหัวกลม
    brushType = BrushType.round;
    // แจ้ง UI
    notifyListeners();
  }
}
