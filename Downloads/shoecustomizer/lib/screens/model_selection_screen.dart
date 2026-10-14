// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าข้อมูลจำลอง
import '../models/mock_data.dart';
// นำเข้าธีมสี
import '../theme/app_theme.dart';
// นำเข้าโลโก้แบรนด์
import '../widgets/brand_logo.dart';
// นำเข้าป้ายราคา PillBadge
import '../widgets/pill_badge.dart';

/// หน้า 3: เลือกรุ่นรองเท้า ตามแบรนด์ที่เลือกมา (brandId)
// หน้าเลือกรุ่นรองเท้า เป็น StatelessWidget
class ModelSelectionScreen extends StatelessWidget {
  // constructor แบบ const
  const ModelSelectionScreen({super.key});

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // อ่าน brandId ที่ถูกส่งมาทาง arguments ของ route แล้วแปลงเป็น String
    final brandId = ModalRoute.of(context)?.settings.arguments as String?;
    // ค้นหาแบรนด์จาก id
    final brand = findBrand(brandId);
    // กรองรุ่นรองเท้าที่เป็นของแบรนด์นี้เท่านั้นแล้วแปลงเป็นลิสต์
    final models = mockModels.where((m) => m.brandId == brandId).toList();

    // Scaffold โครงหน้า
    return Scaffold(
      // CustomScrollView เลื่อนหัวและลิสต์ร่วมกัน
      body: CustomScrollView(
        // รายการ sliver
        slivers: [
          // ส่วนหัวแสดงแบรนด์
          SliverToBoxAdapter(child: _BrandHeader(brand: brand)),
          // พื้นที่มีระยะขอบ
          SliverPadding(
            // ระยะขอบ ซ้าย 20 บน 20 ขวา 20 ล่าง 24
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
            // ลิสต์พร้อมตัวคั่น
            sliver: SliverList.separated(
              // จำนวนรายการเท่าจำนวนรุ่น
              itemCount: models.length,
              // ตัวคั่นเป็นช่องว่างสูง 14
              separatorBuilder: (_, __) => const SizedBox(height: 14),
              // ฟังก์ชันสร้างแต่ละรายการ
              itemBuilder: (context, index) {
                // ดึงรุ่นตามลำดับ
                final model = models[index];
                // สร้างรายการรุ่น
                return _ModelListItem(
                  // ส่งข้อมูลรุ่น
                  model: model,
                  // เมื่อแตะไปหน้า /customizer พร้อมส่ง id รุ่น
                  onTap: () => Navigator.of(context).pushNamed('/customizer', arguments: model.id),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ส่วนหัวของหน้าเลือกรุ่น
class _BrandHeader extends StatelessWidget {
  // แบรนด์ที่เลือก (อาจเป็น null)
  final Brand? brand;

  // constructor ต้องระบุ brand
  const _BrandHeader({required this.brand});

  // เขียนทับ build
  @override
  // build คืนหน้าตาส่วนหัว
  Widget build(BuildContext context) {
    // Container กรอบสีม่วง
    return Container(
      // กว้างเต็มจอ
      width: double.infinity,
      // ระยะขอบใน ด้านบนบวกความสูง status bar
      padding: EdgeInsets.fromLTRB(20, MediaQuery.of(context).padding.top + 16, 20, 32),
      // การตกแต่ง
      decoration: const BoxDecoration(
        // ใช้ heroGradient
        gradient: AppColors.heroGradient,
        // มนเฉพาะมุมล่าง รัศมี 32
        borderRadius: BorderRadius.only(bottomLeft: Radius.circular(32), bottomRight: Radius.circular(32)),
      ),
      // คอลัมน์
      child: Column(
        // จัดชิดซ้าย
        crossAxisAlignment: CrossAxisAlignment.start,
        // รายการลูก
        children: [
          // แถวบนสุด ปุ่มย้อนกลับกับโลโก้
          Row(
            // รายการลูกของแถว
            children: [
              // พื้นหลังปุ่มย้อนกลับ
              Material(
                // ขาวโปร่งใส 18 เปอร์เซ็นต์
                color: Colors.white.withOpacity(0.18),
                // ขอบมน 14
                borderRadius: BorderRadius.circular(14),
                // InkWell จับการแตะ
                child: InkWell(
                  // ริปเปิลขอบมน
                  borderRadius: BorderRadius.circular(14),
                  // เมื่อแตะ ปิดหน้านี้ย้อนกลับไปหน้าก่อนหน้า
                  onTap: () => Navigator.of(context).pop(),
                  // ระยะขอบรอบไอคอน
                  child: const Padding(
                    // ระยะขอบ 10
                    padding: EdgeInsets.all(10),
                    // ไอคอนลูกศรย้อนกลับ สีขาว ขนาด 20
                    child: Icon(Icons.arrow_back, color: Colors.white, size: 20),
                  ),
                ),
              ),
              // Spacer ดันโลโก้ไปชิดขวา
              const Spacer(),
              // ถ้ามีข้อมูลแบรนด์ให้แสดงโลโก้
              if (brand != null)
                // วงกลมพื้นขาว
                Container(
                  // กว้าง 44
                  width: 44,
                  // สูง 44
                  height: 44,
                  // วงกลมสีขาว
                  decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                  // ระยะขอบใน 10
                  padding: const EdgeInsets.all(10),
                  // โลโก้แบรนด์ (เครื่องหมาย ! ยืนยันว่าไม่ใช่ null เพราะเช็กแล้ว)
                  child: BrandLogo(
                    // ลิงก์ SVG
                    url: brand!.logoUrl,
                    // สีแบรนด์
                    color: brand!.brandColor,
                    // อีโมจิสำรอง
                    fallbackEmoji: brand!.emoji,
                    // ขนาด 24
                    size: 24,
                  ),
                ),
            ],
          ),
          // ช่องว่าง 20
          const SizedBox(height: 20),
          // ชื่อแบรนด์
          Text(
            // ถ้า brand เป็น null ใช้คำว่า เลือกรุ่น
            brand?.name ?? 'เลือกรุ่น',
            // สไตล์ headlineMedium
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  // สีขาว
                  color: Colors.white,
                  // ตัวหนา w700
                  fontWeight: FontWeight.w700,
                ),
          ),
          // ช่องว่าง 6
          const SizedBox(height: 6),
          // คำอธิบาย
          Text(
            // เลือกรุ่นรองเท้าที่ต้องการนำไปออกแบบต่อ
            'เลือกรุ่นรองเท้าที่ต้องการนำไปออกแบบต่อ',
            // สไตล์ bodyMedium สีขาวโปร่งใส
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.white.withOpacity(0.85)),
          ),
        ],
      ),
    );
  }
}

// รายการรุ่นรองเท้าหนึ่งแถว
class _ModelListItem extends StatelessWidget {
  // ข้อมูลรุ่น
  final ShoeModel model;
  // ฟังก์ชันเรียกเมื่อแตะ
  final VoidCallback onTap;

  // constructor ต้องระบุ model และ onTap
  const _ModelListItem({required this.model, required this.onTap});

  // เขียนทับ build
  @override
  // build คืนหน้าตารายการ
  Widget build(BuildContext context) {
    // Material พื้นผิว
    return Material(
      // พื้นขาว
      color: AppColors.surface,
      // ขอบมน 22
      borderRadius: BorderRadius.circular(22),
      // InkWell จับแตะ
      child: InkWell(
        // ริปเปิลขอบมน
        borderRadius: BorderRadius.circular(22),
        // เมื่อแตะเรียก onTap
        onTap: onTap,
        // Container ทำเงา
        child: Container(
          // การตกแต่ง
          decoration: BoxDecoration(
            // ขอบมน 22
            borderRadius: BorderRadius.circular(22),
            // เงา
            boxShadow: [
              // เงาหนึ่งชั้น
              BoxShadow(
                // ม่วงโปร่งใส 6 เปอร์เซ็นต์
                color: AppColors.primary.withOpacity(0.06),
                // เบลอ 20
                blurRadius: 20,
                // เลื่อนลง 8
                offset: const Offset(0, 8),
              ),
            ],
          ),
          // ระยะขอบใน 14
          padding: const EdgeInsets.all(14),
          // แถวเรียงไอคอน ข้อมูล ลูกศร
          child: Row(
            // รายการลูก
            children: [
              // กล่องไอคอนรุ่น
              Container(
                // กว้าง 64
                width: 64,
                // สูง 64
                height: 64,
                // ตกแต่ง
                decoration: BoxDecoration(
                  // ไล่สีม่วงหลัก
                  gradient: AppColors.primaryGradient,
                  // ขอบมน 18
                  borderRadius: BorderRadius.circular(18),
                ),
                // จัดกึ่งกลาง
                alignment: Alignment.center,
                // อีโมจิของรุ่น ขนาด 28
                child: Text(model.emoji, style: const TextStyle(fontSize: 28)),
              ),
              // ช่องว่าง 16
              const SizedBox(width: 16),
              // Expanded ให้ข้อความกินพื้นที่ที่เหลือ
              Expanded(
                // คอลัมน์ชื่อรุ่นกับราคา
                child: Column(
                  // จัดชิดซ้าย
                  crossAxisAlignment: CrossAxisAlignment.start,
                  // รายการลูก
                  children: [
                    // ข้อความชื่อรุ่น
                    Text(
                      // ชื่อรุ่น
                      model.name,
                      // สไตล์ titleMedium ตัวหนา w600
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    // ช่องว่าง 8
                    const SizedBox(height: 8),
                    // ป้ายราคาเริ่มต้นเป็นบาท (ใช้การแทรกค่าในสตริง)
                    PillBadge(text: 'เริ่มต้น ${model.basePrice} ฿'),
                  ],
                ),
              ),
              // วงกลมลูกศร
              Container(
                // กว้าง 32
                width: 32,
                // สูง 32
                height: 32,
                // ตกแต่ง สีม่วงอ่อน รูปวงกลม
                decoration: BoxDecoration(color: AppColors.primarySoft, shape: BoxShape.circle),
                // ไอคอนลูกศรขวา ขนาด 18 สีม่วงหลัก
                child: const Icon(Icons.chevron_right, size: 18, color: AppColors.primary),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
