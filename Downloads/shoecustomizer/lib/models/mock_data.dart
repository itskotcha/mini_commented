// นำเข้า Material (ใช้ชนิด Color)
import 'package:flutter/material.dart';
// นำเข้าเฉพาะค่าคงที่พาธโมเดล Nike และ Adidas จาก shoe_parts.dart
import 'shoe_parts.dart' show kShoeAssetNike, kShoeAssetAdidas;

/// ==========================================
///  MODEL CLASSES
/// ==========================================

// คลาสข้อมูลแบรนด์รองเท้า
class Brand {
  // รหัสแบรนด์ เช่น nike
  final String id;
  // ชื่อที่แสดงบนหน้าจอ
  final String name;
  // อีโมจิสำรองกรณีโหลดโลโก้ไม่ได้
  final String emoji; // สำรองไว้กรณีโหลดโลโก้จริงไม่สำเร็จ
  // ลิงก์โลโก้ SVG จริงของแบรนด์
  final String logoUrl; // โลโก้จริงของแบรนด์ (SVG จาก Wikimedia Commons)
  // สีประจำแบรนด์
  final Color brandColor; // สีประจำแบรนด์ ใช้ทำพื้นหลังวงกลมโลโก้
  // ไฟล์โมเดล 3D ที่แบรนด์นี้ใช้
  final String glbAsset; // ไฟล์โมเดล 3D ที่แบรนด์นี้ใช้ (ดู lib/models/shoe_parts.dart)

  // constructor แบบ const
  const Brand({
    // id ต้องระบุ
    required this.id,
    // name ต้องระบุ
    required this.name,
    // emoji ต้องระบุ
    required this.emoji,
    // logoUrl ต้องระบุ
    required this.logoUrl,
    // brandColor ต้องระบุ
    required this.brandColor,
    // glbAsset ต้องระบุ
    required this.glbAsset,
  });
}

// คลาสข้อมูลรุ่นรองเท้า
class ShoeModel {
  // รหัสรุ่น
  final String id;
  // รหัสแบรนด์เจ้าของรุ่น
  final String brandId;
  // ชื่อรุ่น
  final String name;
  // ราคาเริ่มต้นเป็นบาท
  final int basePrice; // ราคาเริ่มต้น (บาท)
  // อีโมจิประจำรุ่น
  final String emoji;

  // constructor แบบ const
  const ShoeModel({
    // id ต้องระบุ
    required this.id,
    // brandId ต้องระบุ
    required this.brandId,
    // name ต้องระบุ
    required this.name,
    // basePrice ต้องระบุ
    required this.basePrice,
    // emoji ไม่บังคับ ค่าเริ่มต้นรูปรองเท้า
    this.emoji = '👟',
  });
}

// คลาสข้อมูลตัวเลือกสีหนึ่งสี
class ColorOption {
  // ชื่อสีภาษาไทย
  final String name;
  // ค่าสีจริง
  final Color color;

  // constructor แบบ const ต้องระบุทั้งชื่อและสี
  const ColorOption({required this.name, required this.color});
}

/// ==========================================
///  MOCK DATA (Hardcoded)
/// ==========================================

// หมายเหตุ: โลโก้ดึงจาก Wikimedia Commons (ไฟล์ SVG สาธารณะ ใช้เพื่ออ้างอิง/นำเสนอ Prototype)
// โมเดล 3D มี 2 ทรง (ดูที่มาใน README.md): Nike/Converse ใช้ทรง "Air Max-like",
// Adidas/Vans ใช้ทรงรองเท้าวิ่งลายเรขาคณิตด้านข้าง — ทั้งสองปรับสีได้ 5 จุดเท่ากัน
// หากทำเป็นแอปจริงเชิงพาณิชย์ ต้องขออนุญาตใช้โลโก้จากเจ้าของแบรนด์ก่อนเสมอ
// รายการแบรนด์ที่ฮาร์ดโค้ดไว้ (ข้อมูลจำลอง แทนฐานข้อมูล)
const List<Brand> mockBrands = [
  // แบรนด์ที่ 1: Nike
  Brand(
    // รหัส nike
    id: 'nike',
    // ชื่อ Nike
    name: 'Nike',
    // อีโมจิสำรอง
    emoji: '✔️',
    // สีประจำแบรนด์ ดำ
    brandColor: Color(0xFF111111),
    // ลิงก์โลโก้ SVG
    logoUrl: 'https://commons.wikimedia.org/wiki/Special:FilePath/Nike_logo_1978.svg',
    // ใช้โมเดล Nike
    glbAsset: kShoeAssetNike,
  ),
  // แบรนด์ที่ 2: Adidas
  Brand(
    // รหัส adidas
    id: 'adidas',
    // ชื่อ Adidas
    name: 'Adidas',
    // อีโมจิสำรอง
    emoji: '🎽',
    // สีประจำแบรนด์ ดำ
    brandColor: Color(0xFF000000),
    // ลิงก์โลโก้ SVG
    logoUrl: 'https://commons.wikimedia.org/wiki/Special:FilePath/Adidas_2022_logo.svg',
    // ใช้โมเดล Adidas
    glbAsset: kShoeAssetAdidas,
  ),
  // แบรนด์ที่ 3: Vans
  Brand(
    // รหัส vans
    id: 'vans',
    // ชื่อ Vans
    name: 'Vans',
    // อีโมจิสำรอง
    emoji: '🛹',
    // สีประจำแบรนด์ แดง
    brandColor: Color(0xFFC8102E),
    // ลิงก์โลโก้ SVG
    logoUrl: 'https://commons.wikimedia.org/wiki/Special:FilePath/Vans_(brand)_logo.svg',
    // Vans ใช้โมเดล Adidas ร่วมกัน
    glbAsset: kShoeAssetAdidas,
  ),
  // แบรนด์ที่ 4: Converse
  Brand(
    // รหัส converse
    id: 'converse',
    // ชื่อ Converse
    name: 'Converse',
    // อีโมจิสำรอง
    emoji: '⭐',
    // สีประจำแบรนด์ เทาเข้ม
    brandColor: Color(0xFF1E1E1E),
    // ลิงก์โลโก้ SVG
    logoUrl: 'https://commons.wikimedia.org/wiki/Special:FilePath/Converse_logo.svg',
    // Converse ใช้โมเดล Nike ร่วมกัน
    glbAsset: kShoeAssetNike,
  ),
];

// รายการรุ่นรองเท้า 8 รุ่น (2 รุ่นต่อแบรนด์)
const List<ShoeModel> mockModels = [
  // Nike Air Max 90 ราคา 3,500 บาท
  ShoeModel(id: 'airmax90', brandId: 'nike', name: 'Air Max 90', basePrice: 3500),
  // Nike Air Force 1 ราคา 3,200 บาท
  ShoeModel(id: 'airforce1', brandId: 'nike', name: 'Air Force 1', basePrice: 3200),
  // Adidas Superstar ราคา 2,900 บาท
  ShoeModel(id: 'superstar', brandId: 'adidas', name: 'Superstar', basePrice: 2900),
  // Adidas Stan Smith ราคา 2,700 บาท
  ShoeModel(id: 'stansmith', brandId: 'adidas', name: 'Stan Smith', basePrice: 2700),
  // Vans Old Skool ราคา 2,200 บาท
  ShoeModel(id: 'oldskool', brandId: 'vans', name: 'Old Skool', basePrice: 2200),
  // Vans Sk8-Hi ราคา 2,450 บาท
  ShoeModel(id: 'sk8hi', brandId: 'vans', name: 'Sk8-Hi', basePrice: 2450),
  // Converse Chuck Taylor All Star ราคา 1,990 บาท
  ShoeModel(id: 'chucktaylor', brandId: 'converse', name: 'Chuck Taylor All Star', basePrice: 1990),
  // Converse Chuck 70 ราคา 2,650 บาท
  ShoeModel(id: 'chuck70', brandId: 'converse', name: 'Chuck 70', basePrice: 2650),
];

// พาเลตสีให้เลือกสำหรับแต่ละชิ้นส่วน (ใช้ร่วมกับ shoeParts ใน shoe_parts.dart)
// พาเลตสี 10 สีให้ผู้ใช้เลือก
const List<ColorOption> mockColors = [
  // สีขาว
  ColorOption(name: 'ขาว', color: Colors.white),
  // สีดำ
  ColorOption(name: 'ดำ', color: Color(0xFF1B1B1B)),
  // สีแดง
  ColorOption(name: 'แดง', color: Color(0xFFE53935)),
  // สีน้ำเงิน
  ColorOption(name: 'น้ำเงิน', color: Color(0xFF1E88E5)),
  // สีเขียว
  ColorOption(name: 'เขียว', color: Color(0xFF43A047)),
  // สีเหลือง
  ColorOption(name: 'เหลือง', color: Color(0xFFFDD835)),
  // สีส้ม
  ColorOption(name: 'ส้ม', color: Color(0xFFFB8C00)),
  // สีชมพู
  ColorOption(name: 'ชมพู', color: Color(0xFFEC407A)),
  // สีเทา
  ColorOption(name: 'เทา', color: Color(0xFF9E9E9E)),
  // สีน้ำตาล
  ColorOption(name: 'น้ำตาล', color: Color(0xFF6D4C41)),
];

// ฟังก์ชันช่วยหา Brand จาก id
// หาแบรนด์จากรหัส
Brand? findBrand(String? brandId) {
  // ถ้า id เป็น null คืน null ทันที
  if (brandId == null) return null;
  // ใช้ try เพราะ firstWhere จะโยน exception ถ้าไม่พบ
  try {
    // คืนแบรนด์แรกที่ id ตรงกัน
    return mockBrands.firstWhere((b) => b.id == brandId);
  // ถ้าไม่พบ (เกิด exception)
  } catch (_) {
    // คืน null แทน
    return null;
  }
}

// ฟังก์ชันช่วยหา Model จาก id
// หารุ่นจากรหัส
ShoeModel? findModel(String? modelId) {
  // ถ้า id เป็น null คืน null
  if (modelId == null) return null;
  // ใช้ try เพื่อรับมือกรณีไม่พบ
  try {
    // คืนรุ่นแรกที่ id ตรงกัน
    return mockModels.firstWhere((m) => m.id == modelId);
  // ไม่พบ
  } catch (_) {
    // คืน null
    return null;
  }
}

// ฟังก์ชันช่วยหาชื่อสีจากค่าสี (ใช้ในหน้าสรุป)
// หาชื่อสีจากค่าสี สำหรับแสดงในหน้าสรุป
String colorName(Color color) {
  // กรองหาสีในพาเลตที่ค่า value ตรงกัน
  final match = mockColors.where((c) => c.color.value == color.value);
  // ถ้าเจอคืนชื่อสีแรก ถ้าไม่เจอ (เช่น เลือกจากวงล้อสี) คืนคำว่า กำหนดเอง
  return match.isNotEmpty ? match.first.name : 'กำหนดเอง';
}
