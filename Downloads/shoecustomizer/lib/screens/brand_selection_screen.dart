// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าข้อมูลแบรนด์จำลอง
import '../models/mock_data.dart';
// นำเข้าธีมสี
import '../theme/app_theme.dart';
// นำเข้าวิดเจ็ตโลโก้แบรนด์
import '../widgets/brand_logo.dart';

/// หน้า 2: เลือกแบรนด์รองเท้า
/// Hero header ไล่เฉดสีม่วงด้านบน + การ์ดขาวโค้งมนแสดงโลโก้แบรนด์จริงในกริด 2 คอลัมน์
// หน้าเลือกแบรนด์ เป็น StatelessWidget
class BrandSelectionScreen extends StatelessWidget {
  // constructor แบบ const
  const BrandSelectionScreen({super.key});

  // เขียนทับ build
  @override
  // build คืนหน้าตาทั้งหน้า
  Widget build(BuildContext context) {
    // Scaffold โครงหน้า
    return Scaffold(
      // CustomScrollView ให้เลื่อนส่วนหัวและกริดรวมกันเป็นผืนเดียว
      body: CustomScrollView(
        // รายการ sliver ที่ประกอบกันในพื้นที่เลื่อน
        slivers: [
          // sliver แรกคือส่วนหัวสีม่วง
          SliverToBoxAdapter(child: _HeroHeader()),
          // sliver ที่สองเป็นพื้นที่มีระยะขอบ
          SliverPadding(
            // ระยะขอบ ซ้าย 20 บน 24 ขวา 20 ล่าง 24
            padding: const EdgeInsets.fromLTRB(20, 24, 20, 24),
            // กริดแสดงการ์ดแบรนด์
            sliver: SliverGrid(
              // ตัวกำหนดรูปแบบกริด
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                // 2 คอลัมน์
                crossAxisCount: 2,
                // ระยะห่างแนวตั้ง 16
                mainAxisSpacing: 16,
                // ระยะห่างแนวนอน 16
                crossAxisSpacing: 16,
                // อัตราส่วนกว้างต่อสูงของการ์ด
                childAspectRatio: 0.95,
              ),
              // ตัวสร้างลูกของกริดแบบสร้างเมื่อต้องแสดง
              delegate: SliverChildBuilderDelegate(
                // ฟังก์ชันสร้างการ์ดตามลำดับ index
                (context, index) {
                  // ดึงแบรนด์ตามลำดับ
                  final brand = mockBrands[index];
                  // สร้างการ์ดแบรนด์
                  return _BrandCard(
                    // ส่งข้อมูลแบรนด์
                    brand: brand,
                    // เมื่อแตะให้ไปหน้า /models พร้อมส่ง id แบรนด์เป็น arguments
                    onTap: () => Navigator.of(context).pushNamed('/models', arguments: brand.id),
                  );
                },
                // จำนวนการ์ดเท่ากับจำนวนแบรนด์
                childCount: mockBrands.length,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ส่วนหัวสีม่วงของหน้า (คลาส private ขึ้นต้นด้วย _)
class _HeroHeader extends StatelessWidget {
  // เขียนทับ build
  @override
  // build คืนหน้าตาส่วนหัว
  Widget build(BuildContext context) {
    // Container กรอบสีม่วง
    return Container(
      // กว้างเต็มจอ
      width: double.infinity,
      // ระยะขอบใน ด้านบนบวกความสูง status bar ของเครื่อง เพื่อไม่ให้ชนแถบสถานะ
      padding: EdgeInsets.fromLTRB(24, MediaQuery.of(context).padding.top + 24, 24, 40),
      // การตกแต่ง
      decoration: const BoxDecoration(
        // ใช้ heroGradient
        gradient: AppColors.heroGradient,
        // มนเฉพาะมุมล่างซ้ายและขวา รัศมี 36
        borderRadius: BorderRadius.only(bottomLeft: Radius.circular(36), bottomRight: Radius.circular(36)),
      ),
      // คอลัมน์เรียงข้อความ
      child: Column(
        // จัดชิดซ้าย
        crossAxisAlignment: CrossAxisAlignment.start,
        // รายการลูก
        children: [
          // แถวบนสุด ไอคอนทักทายกับข้อความ
          Row(
            // รายการลูกของแถว
            children: [
              // กล่องไอคอนทักทาย
              Container(
                // กว้าง 40
                width: 40,
                // สูง 40
                height: 40,
                // การตกแต่ง
                decoration: BoxDecoration(
                  // พื้นขาวโปร่งใส 18 เปอร์เซ็นต์
                  color: Colors.white.withOpacity(0.18),
                  // ขอบมน 12
                  borderRadius: BorderRadius.circular(12),
                ),
                // จัดกึ่งกลาง
                alignment: Alignment.center,
                // แสดงอีโมจิโบกมือ
                child: const Text('👋', style: TextStyle(fontSize: 20)),
              ),
              // ช่องว่างแนวนอน 12
              const SizedBox(width: 12),
              // ข้อความทักทาย
              Text(
                // สวัสดี นักออกแบบ
                'สวัสดี นักออกแบบ',
                // สไตล์ bodyMedium สีขาวโปร่งใส
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.white.withOpacity(0.85)),
              ),
            ],
          ),
          // ช่องว่างแนวตั้ง 16
          const SizedBox(height: 16),
          // หัวข้อใหญ่
          Text(
            // ข้อความสองบรรทัด (ใช้ \n เพื่อขึ้นบรรทัดใหม่)
            'เลือกแบรนด์\nที่คุณชื่นชอบ',
            // สไตล์ headlineMedium
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  // สีขาว
                  color: Colors.white,
                  // ตัวหนา w700
                  fontWeight: FontWeight.w700,
                  // ความสูงบรรทัด 1.25 เท่า
                  height: 1.25,
                ),
          ),
          // ช่องว่างแนวตั้ง 8
          const SizedBox(height: 8),
          // ข้อความคำโปรย
          Text(
            // เริ่มต้นออกแบบรองเท้าคู่พิเศษของคุณได้เลย
            'เริ่มต้นออกแบบรองเท้าคู่พิเศษของคุณได้เลย',
            // สไตล์ bodyMedium สีขาวโปร่งใส
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.white.withOpacity(0.85)),
          ),
        ],
      ),
    );
  }
}

// การ์ดแบรนด์หนึ่งใบในกริด
class _BrandCard extends StatelessWidget {
  // ข้อมูลแบรนด์ที่จะแสดง
  final Brand brand;
  // ฟังก์ชันเรียกเมื่อแตะการ์ด
  final VoidCallback onTap;

  // constructor ต้องระบุทั้ง brand และ onTap
  const _BrandCard({required this.brand, required this.onTap});

  // เขียนทับ build
  @override
  // build คืนหน้าตาการ์ด
  Widget build(BuildContext context) {
    // Material พื้นผิวสำหรับ InkWell
    return Material(
      // สีพื้นขาว
      color: AppColors.surface,
      // ขอบมน 24
      borderRadius: BorderRadius.circular(24),
      // ไม่มีเงาจาก Material (เงาทำเองใน Container)
      elevation: 0,
      // InkWell จับการแตะและแสดงริปเปิล
      child: InkWell(
        // ริปเปิลมีขอบมน
        borderRadius: BorderRadius.circular(24),
        // เมื่อแตะเรียก onTap
        onTap: onTap,
        // Container ทำเงาและโครงสร้างภายใน
        child: Container(
          // การตกแต่ง
          decoration: BoxDecoration(
            // ขอบมน 24
            borderRadius: BorderRadius.circular(24),
            // เงา
            boxShadow: [
              // เงาหนึ่งชั้น
              BoxShadow(
                // สีม่วงโปร่งใส 8 เปอร์เซ็นต์
                color: AppColors.primary.withOpacity(0.08),
                // เบลอ 24
                blurRadius: 24,
                // เลื่อนลง 10
                offset: const Offset(0, 10),
              ),
            ],
          ),
          // คอลัมน์เรียงโลโก้กับชื่อ
          child: Column(
            // จัดกึ่งกลางแนวตั้ง
            mainAxisAlignment: MainAxisAlignment.center,
            // รายการลูก
            children: [
              // วงกลมพื้นหลังโลโก้
              Container(
                // กว้าง 68
                width: 68,
                // สูง 68
                height: 68,
                // ตกแต่ง
                decoration: BoxDecoration(
                  // สีม่วงอ่อน
                  color: AppColors.primarySoft,
                  // รูปวงกลม
                  shape: BoxShape.circle,
                ),
                // ระยะขอบใน 16
                padding: const EdgeInsets.all(16),
                // โลโก้แบรนด์
                child: BrandLogo(
                  // ลิงก์ SVG
                  url: brand.logoUrl,
                  // สีโลโก้ตามสีแบรนด์
                  color: brand.brandColor,
                  // อีโมจิสำรอง
                  fallbackEmoji: brand.emoji,
                  // ขนาด 36
                  size: 36,
                ),
              ),
              // ช่องว่าง 14
              const SizedBox(height: 14),
              // ชื่อแบรนด์
              Text(
                // ข้อความชื่อ
                brand.name,
                // สไตล์ titleMedium ตัวหนา w600
                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
