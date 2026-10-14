/// ==========================================
///  SHOE 3D PARTS
/// ==========================================
///
/// แอปนี้มีโมเดล 3D จริง 2 ทรง (ดูรายละเอียดที่มาใน README.md):
///
/// - "assets/shoe_nike.glb"   (ใช้กับ Nike, Converse) — 14 material
/// - "assets/shoe_adidas.glb" (ใช้กับ Adidas, Vans)   — 8 material
///
/// ทั้งสองโมเดลถูก mapping ให้มี "ชิ้นส่วนที่ปรับสีได้" 5 ชิ้นเหมือนกัน
/// (ตัวรองเท้า, ลายด้านข้าง, พื้นรองเท้า, เชือกรองเท้า, โลโก้)
/// เพื่อให้ UI สม่ำเสมอกันทุกแบรนด์ แม้ material index จริงจะต่างกันไปตามไฟล์
// คลาสแทน 'ชิ้นส่วนที่ปรับสีได้' หนึ่งชิ้นของรองเท้า (เช่น พื้น เชือก)
class ShoePart3D {
  // รหัสชิ้นส่วน เช่น body, sole
  final String id;
  // ชื่อภาษาไทยที่แสดงให้ผู้ใช้เห็น
  final String label;

  /// ดัชนี material (ตามลำดับใน gltf.materials[]) ที่อยู่ในชิ้นส่วนนี้ — เฉพาะไฟล์ .glb ที่ mapping นี้ผูกไว้
  // รายการเลข material ในไฟล์ GLB ที่อยู่ในชิ้นส่วนนี้ (ชิ้นส่วนหนึ่งอาจประกอบหลาย material)
  final List<int> materialIndices;

  // constructor แบบ const (สร้างตอนคอมไพล์ได้)
  const ShoePart3D({
    // id ต้องระบุ
    required this.id,
    // label ต้องระบุ
    required this.label,
    // materialIndices ต้องระบุ
    required this.materialIndices,
  });
}

/// Path ของโมเดลแต่ละทรง
// พาธไฟล์โมเดล 3D ของ Nike (ใช้กับ Nike และ Converse)
const String kShoeAssetNike = 'assets/shoe_nike.glb';
// พาธไฟล์โมเดล 3D ของ Adidas (ใช้กับ Adidas และ Vans)
const String kShoeAssetAdidas = 'assets/shoe_adidas.glb';

/// mapping สำหรับ assets/shoe_nike.glb
/// (แกะจากไฟล์จริง: Upper, Upper.001, Upper.002, Main, BottumBack, BottumBack.001,
///  BottumBack.002, Material.001, Middle Sole, Middle Sole.001, SIDEDESIGN,
///  SIDEDESIGN.001, Back(Brick), Bottum Sole)
// ตารางจับคู่ชิ้นส่วนสำหรับโมเดล Nike (14 material)
const List<ShoePart3D> _shoePartsNike = [
  // ตัวรองเท้า ประกอบจาก material 0,2,3,6,10
  ShoePart3D(id: 'body', label: 'ตัวรองเท้า', materialIndices: [0, 2, 3, 6, 10]),
  // ลายด้านข้าง material 8,9
  ShoePart3D(id: 'side', label: 'ลายด้านข้าง', materialIndices: [8, 9]),
  // พื้นรองเท้า material 1,4,5,12,13
  ShoePart3D(id: 'sole', label: 'พื้นรองเท้า', materialIndices: [1, 4, 5, 12, 13]),
  // เชือกรองเท้า material 11
  ShoePart3D(id: 'laces', label: 'เชือกรองเท้า', materialIndices: [11]),
  // โลโก้ material 7
  ShoePart3D(id: 'logo', label: 'โลโก้', materialIndices: [7]),
];

/// mapping สำหรับ assets/shoe_adidas.glb
/// (แกะจากไฟล์จริง: material index 0=laces, 1=mesh, 2=caps, 3=inner, 4=sole,
///  5=stripes, 6=band, 7=patch)
// ตารางจับคู่ชิ้นส่วนสำหรับโมเดล Adidas (8 material)
const List<ShoePart3D> _shoePartsAdidas = [
  // ตัวรองเท้าใช้ material 1 (mesh)
  ShoePart3D(id: 'body', label: 'ตัวรองเท้า', materialIndices: [1]),
  // ลายข้างใช้ material 5 (stripes)
  ShoePart3D(id: 'side', label: 'ลายด้านข้าง', materialIndices: [5]),
  // พื้นใช้ material 4 (sole)
  ShoePart3D(id: 'sole', label: 'พื้นรองเท้า', materialIndices: [4]),
  // เชือกใช้ material 0 (laces)
  ShoePart3D(id: 'laces', label: 'เชือกรองเท้า', materialIndices: [0]),
  // โลโก้รวม material 2,3,6,7 (caps, inner, band, patch)
  ShoePart3D(id: 'logo', label: 'โลโก้', materialIndices: [2, 3, 6, 7]),
];

/// ใช้รายการนี้เป็น "ตัวกำหนด id/label มาตรฐาน" สำหรับ init ค่าสีเริ่มต้นใน CustomizerState
/// (ทั้งสองโมเดลใช้ id ชุดเดียวกัน: body, side, sole, laces, logo)
// รายการมาตรฐานของชิ้นส่วน ใช้กำหนด id เริ่มต้นใน CustomizerState (ใช้ชุดของ Nike เป็นตัวแทน เพราะ id ตรงกันทั้งสองโมเดล)
const List<ShoePart3D> shoeParts = _shoePartsNike;

/// คืนรายการชิ้นส่วนที่ถูกต้องตามไฟล์โมเดลที่ระบุ
// ฟังก์ชันคืนรายการชิ้นส่วนที่ถูกต้องตามไฟล์โมเดลที่ส่งเข้ามา
List<ShoePart3D> partsForAsset(String assetPath) {
  // เลือกตามพาธไฟล์
  switch (assetPath) {
    // ถ้าเป็นไฟล์ Adidas
    case kShoeAssetAdidas:
      // คืนชุดชิ้นส่วนของ Adidas
      return _shoePartsAdidas;
    // ถ้าเป็นไฟล์ Nike
    case kShoeAssetNike:
    // หรือกรณีอื่นทั้งหมด (default) ให้ใช้ชุดของ Nike
    default:
      // คืนชุดชิ้นส่วนของ Nike
      return _shoePartsNike;
  }
}

// ฟังก์ชันค้นหาชิ้นส่วนจาก id ในรายการ คืน null ถ้าไม่พบ
ShoePart3D? findShoePart(List<ShoePart3D> parts, String id) {
  // วนดูทีละชิ้นส่วน
  for (final part in parts) {
    // ถ้า id ตรงกันให้คืนชิ้นส่วนนั้นทันที
    if (part.id == id) return part;
  }
  // วนครบแล้วไม่พบ คืน null
  return null;
}
