// นำเข้า dart:typed_data สำหรับ Uint8List (ไบต์รูปสติกเกอร์)
import 'dart:typed_data';
// นำเข้าเฉพาะ FontFeature จาก dart:ui (ใช้ทำตัวเลขกว้างเท่ากัน)
import 'dart:ui' show FontFeature;

// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้าวงล้อสีจากแพ็กเกจ flutter_colorpicker
import 'package:flutter_colorpicker/flutter_colorpicker.dart';
// นำเข้า image_picker สำหรับเลือกรูปจากแกลเลอรี
import 'package:image_picker/image_picker.dart';
// นำเข้า provider (context.read, Consumer)
import 'package:provider/provider.dart';

// นำเข้าโมเดลสติกเกอร์/เส้นวาด/enum
import '../models/custom_design.dart';
// นำเข้าข้อมูลจำลอง (findModel, findBrand)
import '../models/mock_data.dart';
// นำเข้าข้อมูลชิ้นส่วนรองเท้า
import '../models/shoe_parts.dart';
// นำเข้า CustomizerState
import '../state/customizer_state.dart';
// นำเข้าธีมสี
import '../theme/app_theme.dart';
// นำเข้าปุ่มไล่สี
import '../widgets/gradient_button.dart';
// นำเข้าตัวแสดงโมเดล 3D
import '../widgets/shoe_model_viewer.dart';

/// หน้า 3D Customization Studio
/// รองรับปรับสี, นำเข้าสติกเกอร์ และวาดลายบนพื้นที่รองเท้า
// หน้าตกแต่งรองเท้า เป็น StatefulWidget เพราะมีสถานะเครื่องมือ สติกเกอร์ที่เลือก ฯลฯ
class CustomizerScreen extends StatefulWidget {
  // constructor แบบ const
  const CustomizerScreen({super.key});

  // เขียนทับ createState
  @override
  // สร้าง State ของหน้านี้
  State<CustomizerScreen> createState() => _CustomizerScreenState();
}

// คลาส State ของหน้าตกแต่ง
class _CustomizerScreenState extends State<CustomizerScreen> {
  // ธงว่าตั้งค่ารุ่นรองเท้าลง state แล้วหรือยัง (กันทำซ้ำทุกครั้งที่ build)
  bool _initialized = false;
  // GlobalKey ใช้สั่งเมธอดของ ShoeModelViewerState จากภายนอก (เช่น applyColor)
  final GlobalKey<ShoeModelViewerState> _viewerKey = GlobalKey<ShoeModelViewerState>();
  // ตัวเลือกรูปจากแกลเลอรี
  final ImagePicker _imagePicker = ImagePicker();
  // เครื่องมือปัจจุบัน เริ่มที่เปลี่ยนสี
  CustomizerTool _tool = CustomizerTool.color;
  // id สติกเกอร์ที่ถูกเลือกอยู่ (null คือไม่ได้เลือก)
  String? _selectedStickerId;
  // id สติกเกอร์ที่รอผู้ใช้แตะวาง
  String? _pendingStickerId;
  // ไบต์รูปของสติกเกอร์ที่รอวาง
  Uint8List? _pendingStickerBytes;

  // นำเข้ารูปจากแกลเลอรี
  Future<void> _importSticker() async {
    // ใช้ try ดัก error
    try {
      // เปิดตัวเลือกรูปและรอผู้ใช้เลือก
      final image = await _imagePicker.pickImage(
        // เลือกจากแกลเลอรี
        source: ImageSource.gallery,
        // บีบอัดคุณภาพ 85 เปอร์เซ็นต์
        imageQuality: 85,
        // ย่อความกว้างไม่เกิน 900 พิกเซล เพื่อลดขนาด texture
        maxWidth: 900,
      );
      // ถ้าผู้ใช้ยกเลิกหรือหน้านี้ถูกถอดแล้ว ให้ออก
      if (image == null || !mounted) return;
      // อ่านไฟล์รูปเป็นไบต์
      final bytes = await image.readAsBytes();
      // ตรวจ mounted อีกครั้งหลัง await
      if (!mounted) return;
      // เตรียมสติกเกอร์ด้วยไบต์นี้
      _prepareSticker(bytes);
    // ถ้าเปิดรูปไม่ได้
    } catch (_) {
      // ถ้าหน้าถูกถอดแล้วให้ออก
      if (!mounted) return;
      // แสดง SnackBar แจ้งเตือน
      ScaffoldMessenger.of(context).showSnackBar(
        // ข้อความ ไม่สามารถเปิดรูปนี้ได้ ให้ลองรูปอื่น
        const SnackBar(content: Text('ไม่สามารถเปิดรูปนี้ได้ กรุณาลองเลือกรูปอื่น')),
      );
    }
  }

  // เตรียมสติกเกอร์ที่เพิ่งเลือก
  void _prepareSticker(Uint8List bytes) {
    // สร้าง id ไม่ซ้ำจากเวลาปัจจุบัน (หน่วยไมโครวินาที)
    final id = DateTime.now().microsecondsSinceEpoch.toString();
    // อัปเดตสถานะของหน้า
    setState(() {
      // สลับเป็นเครื่องมือสติกเกอร์
      _tool = CustomizerTool.sticker;
      // ยกเลิกการเลือกสติกเกอร์เดิม
      _selectedStickerId = null;
      // จำ id สติกเกอร์ที่รอวาง
      _pendingStickerId = id;
      // จำไบต์รูปที่รอวาง
      _pendingStickerBytes = bytes;
    });
    // บอก WebView ให้เปลี่ยนเป็นโหมดสติกเกอร์
    _viewerKey.currentState?.setEditorTool(CustomizerTool.sticker);
    // ส่งรูปไปรอไว้ใน WebView เพื่อรอผู้ใช้แตะวาง
    _viewerKey.currentState?.prepareSticker(id, bytes);
    // แสดง SnackBar
    ScaffoldMessenger.of(context).showSnackBar(
      // ข้อความ แตะตำแหน่งบนผิวรองเท้าเพื่อวางสติกเกอร์
      const SnackBar(content: Text('แตะตำแหน่งบนผิวรองเท้าเพื่อวางสติกเกอร์')),
    );
  }

  // เรียกเมื่อ WebView แจ้งว่าวางสติกเกอร์แล้ว
  void _handleStickerPlaced(String id, String surface) {
    // ถ้า id ไม่ตรงกับที่รอวาง หรือไม่มีไบต์ ให้ข้าม
    if (_pendingStickerId != id || _pendingStickerBytes == null) return;
    // เพิ่มสติกเกอร์ลง CustomizerState
    context.read<CustomizerState>().addSticker(
          // id
          id: id,
          // ไบต์รูป
          imageBytes: _pendingStickerBytes!,
          // ตำแหน่งบนผิวที่ WebView ส่งมา
          surface: surface,
        );
    // อัปเดตสถานะของหน้า
    setState(() {
      // เลือกสติกเกอร์ที่เพิ่งวาง
      _selectedStickerId = id;
      // เคลียร์สถานะรอวาง
      _pendingStickerId = null;
      // เคลียร์ไบต์ที่รอวาง
      _pendingStickerBytes = null;
    });
  }

  // อัปเดตสติกเกอร์ที่เลือกอยู่ (ขนาดหรือมุมหมุน)
  void _updateSelectedSticker(
    // รับ state
    CustomizerState state, {
    // ขนาดใหม่ (ถ้ามี)
    double? scale,
    // มุมใหม่ (ถ้ามี)
    double? rotation,
  }) {
    // ดึง id ที่เลือกอยู่
    final id = _selectedStickerId;
    // ถ้าไม่ได้เลือกอะไรให้ออก
    if (id == null) return;
    // อัปเดตค่าใน state
    state.updateSticker(id, scale: scale, rotation: rotation);
    // หาสติกเกอร์ตัวที่ถูกอัปเดตแล้ว
    final matches = state.stickers.where((item) => item.id == id);
    // ถ้าพบ
    if (matches.isNotEmpty) {
      // ส่งค่าใหม่ไปวาดใหม่ใน WebView
      _viewerKey.currentState?.updateStickerTransform(matches.first);
    }
  }

  // เขียนทับ build
  @override
  // build คืนหน้าตาทั้งหน้า
  Widget build(BuildContext context) {
    // อ่านรหัสรุ่นที่ส่งมาทาง arguments ของ route
    final modelId = ModalRoute.of(context)?.settings.arguments as String?;
    // หารุ่นรองเท้า
    final model = findModel(modelId);
    // หาแบรนด์ของรุ่นนี้
    final brand = findBrand(model?.brandId);
    // พาธไฟล์โมเดล 3D ของแบรนด์ (ถ้าไม่มีใช้ Nike)
    final assetPath = brand?.glbAsset ?? kShoeAssetNike;
    // รายการชิ้นส่วนของโมเดลนี้
    final parts = partsForAsset(assetPath);

    // ถ้ายังไม่ได้ตั้งค่ารุ่นและมี modelId
    if (!_initialized && modelId != null) {
      // ตั้งธงว่าตั้งแล้ว
      _initialized = true;
      // รอให้ build เฟรมนี้เสร็จก่อนแล้วค่อยเรียก (ห้ามเปลี่ยน state ระหว่าง build)
      WidgetsBinding.instance.addPostFrameCallback((_) {
        // ตั้งรุ่นรองเท้าลง CustomizerState ถ้าหน้ายังอยู่
        if (mounted) context.read<CustomizerState>().setModel(modelId);
      });
    }

    // อ่าน state ครั้งเดียว (read ไม่ทำให้ rebuild)
    final state = context.read<CustomizerState>();
    // หมุนกล้องได้เฉพาะตอนใช้เครื่องมือสี (โหมดสติกเกอร์/วาดต้องล็อกกล้องเพื่อให้ลากบนผิวได้)
    final canRotateModel = _tool == CustomizerTool.color;

    // Scaffold โครงหน้า
    return Scaffold(
      // AppBar แสดงชื่อรุ่น ถ้าไม่มีใช้ข้อความสำรอง
      appBar: AppBar(title: Text(model?.name ?? 'ออกแบบรองเท้า')),
      // ตัวหน้าเป็นคอลัมน์ แบ่งส่วนเลื่อนกับปุ่มถัดไป
      body: Column(
        // รายการลูก
        children: [
          // Expanded ให้ส่วนเลื่อนกินพื้นที่ที่เหลือ
          Expanded(
            // เลื่อนเนื้อหาได้
            child: SingleChildScrollView(
              // คอลัมน์เนื้อหา
              child: Column(
                // รายการลูก
                children: [
                  // กล่องโมเดล 3D มีขอบไล่สี
                  Container(
                    // กว้างเต็ม
                    width: double.infinity,
                    // สูง 280
                    height: 280,
                    // ระยะขอบนอก ซ้าย 20 บน 4 ขวา 20 ล่าง 0
                    margin: const EdgeInsets.fromLTRB(20, 4, 20, 0),
                    // ตกแต่ง
                    decoration: BoxDecoration(
                      // ไล่สีม่วงเป็นขอบ
                      gradient: AppColors.primaryGradient,
                      // ขอบมน 28
                      borderRadius: BorderRadius.circular(28),
                      // เงา
                      boxShadow: [
                        // เงาหนึ่งชั้น
                        BoxShadow(
                          // ม่วงโปร่งใส 25 เปอร์เซ็นต์
                          color: AppColors.primary.withOpacity(0.25),
                          // เบลอ 30
                          blurRadius: 30,
                          // เลื่อนลง 14
                          offset: const Offset(0, 14),
                        ),
                      ],
                    ),
                    // ระยะขอบใน 3 ทำให้เห็นขอบไล่สีบาง ๆ
                    child: Padding(
                      // ปรับระยะขอบ
                      padding: const EdgeInsets.all(3),
                      // ClipRRect ตัดเนื้อหาให้มนตาม
                      child: ClipRRect(
                        // รัศมี 26
                        borderRadius: BorderRadius.circular(26),
                        // พื้นหลังสีขาว
                        child: ColoredBox(
                          // สีขาว
                          color: Colors.white,
                          // Stack ซ้อนโมเดลกับป้ายบอกโหมด
                          child: Stack(
                            // ให้ลูกขยายเต็ม
                            fit: StackFit.expand,
                            // รายการลูก
                            children: [
                              // ตัวแสดงโมเดล 3D
                              ShoeModelViewer(
                                // ผูก GlobalKey เพื่อสั่งการจากภายนอก
                                key: _viewerKey,
                                // ไฟล์โมเดล
                                assetPath: assetPath,
                                // รายการชิ้นส่วน
                                parts: parts,
                                // สีเริ่มต้นจากสถานะ
                                initialColors: state.partColors,
                                // เปิดให้หมุนกล้องเฉพาะโหมดสี
                                cameraControls: canRotateModel,
                                // ปิดหมุนอัตโนมัติ
                                autoRotate: false,
                                // เปิดโหมดแก้ไขลวดลาย
                                decorationEditable: true,
                                // เครื่องมือปัจจุบัน
                                tool: _tool,
                                // สีปากกา
                                brushColor: state.brushColor,
                                // ขนาดปากกา
                                brushSize: state.brushSize,
                                // ชนิดหัวปากกา
                                brushType: state.brushType,
                                // รายการสติกเกอร์
                                stickers: state.stickers,
                                // รายการเส้นวาด
                                drawingStrokes: state.drawingStrokes,
                                // id สติกเกอร์ที่เลือก
                                selectedStickerId: _selectedStickerId,
                                // เมื่อ WebView แจ้งว่ามีการเลือกสติกเกอร์
                                onStickerSelected: (id) {
                                  // เก็บ id ที่เลือกลงสถานะ
                                  setState(() => _selectedStickerId = id);
                                },
                                // เมื่อวางสติกเกอร์ ให้เรียก _handleStickerPlaced
                                onStickerPlaced: _handleStickerPlaced,
                                // เมื่อย้ายสติกเกอร์
                                onStickerMoved: (id, surface) {
                                  // อัปเดตตำแหน่งลง state
                                  state.updateSticker(id, surface: surface);
                                },
                                // เมื่อเริ่มลากเส้น ให้เรียกเมธอดใน state ตรง ๆ
                                onStrokeStarted: state.startSurfaceStroke,
                                // เมื่อมีจุดใหม่ต่อท้ายเส้น ให้เรียกเมธอดใน state ตรง ๆ
                                onStrokePointAdded: state.appendSurfaceStrokePoint,
                              ),
                              // Positioned วางป้ายบอกโหมดมุมขวาบน
                              Positioned(
                                // ห่างขอบบน 12
                                top: 12,
                                // ห่างขอบขวา 12
                                right: 12,
                                // ป้ายบอกโหมดและวิธีใช้
                                child: _ModeHint(tool: _tool, canRotateModel: canRotateModel),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                  // ช่องว่าง 16
                  const SizedBox(height: 16),
                  // แถบเลือกเครื่องมือ สี สติกเกอร์ ปากกา
                  _ToolSelector(
                    // เครื่องมือที่เลือกอยู่
                    selectedTool: _tool,
                    // เมื่อเลือกเครื่องมือใหม่
                    onSelected: (tool) {
                      // อัปเดตสถานะ
                      setState(() {
                        // เก็บเครื่องมือใหม่
                        _tool = tool;
                        // ถ้าไม่ใช่โหมดสติกเกอร์ ให้ยกเลิกการเลือกสติกเกอร์
                        if (tool != CustomizerTool.sticker) _selectedStickerId = null;
                      });
                      // แจ้ง WebView ให้เปลี่ยนโหมด
                      _viewerKey.currentState?.setEditorTool(tool);
                    },
                  ),
                  // ช่องว่าง 18
                  const SizedBox(height: 18),
                  // AnimatedSwitcher ทำเอฟเฟกต์ค่อย ๆ สลับแผงเครื่องมือ
                  AnimatedSwitcher(
                    // ใช้เวลา 220 มิลลิวินาที
                    duration: const Duration(milliseconds: 220),
                    // Consumer ฟังการเปลี่ยนแปลงของ CustomizerState เฉพาะส่วนนี้
                    child: Consumer<CustomizerState>(
                      // เลือกแผงตามเครื่องมือปัจจุบันด้วย switch expression
                      builder: (context, liveState, _) => switch (_tool) {
                        // ถ้าเป็นเครื่องมือสี แสดงแผงสี
                        CustomizerTool.color => _buildColorPanel(
                            // ส่ง context
                            context,
                            // ส่ง state ล่าสุด
                            liveState,
                            // ส่งรายการชิ้นส่วน
                            parts,
                            // ส่งสีปัจจุบันของชิ้นส่วนที่เลือก
                            liveState.colorOf(liveState.selectedPartId),
                          ),
                        // ถ้าเป็นเครื่องมือสติกเกอร์ แสดงแผงสติกเกอร์
                        CustomizerTool.sticker => _buildStickerPanel(context, liveState),
                        // ถ้าเป็นเครื่องมือวาด แสดงแผงปากกา
                        CustomizerTool.draw => _buildDrawingPanel(context, liveState),
                      },
                    ),
                  ),
                  // ช่องว่าง 12
                  const SizedBox(height: 12),
                ],
              ),
            ),
          ),
          // ระยะขอบรอบปุ่มถัดไป
          Padding(
            // ระยะขอบ 20
            padding: const EdgeInsets.all(20),
            // ปุ่มถัดไปแบบไล่สี
            child: GradientButton(
              // ข้อความ ถัดไป
              label: 'ถัดไป',
              // ไอคอนลูกศรไปข้างหน้า
              trailingIcon: Icons.arrow_forward_rounded,
              // เมื่อกดไปหน้า /summary
              onPressed: () => Navigator.of(context).pushNamed('/summary'),
            ),
          ),
        ],
      ),
    );
  }

  // สร้างแผงเปลี่ยนสี
  Widget _buildColorPanel(
    // ตัวแปร context
    BuildContext context,
    // ตัวแปร state
    CustomizerState state,
    // ตัวแปรรายการชิ้นส่วน
    List<ShoePart3D> parts,
    // ตัวแปรสีที่เลือกอยู่
    Color selectedColor,
  ) {
    // คอลัมน์ของแผง
    return Column(
      // key ใช้ให้ AnimatedSwitcher แยกแผงนี้ออกจากแผงอื่นตอนสลับ
      key: const ValueKey('color-panel'),
      // รายการลูก
      children: [
        // ระยะขอบแนวนอน 20
        Padding(
          // ระยะขอบ
          padding: const EdgeInsets.symmetric(horizontal: 20),
          // Align จัดชิดซ้าย
          child: Align(
            // ชิดกลางซ้าย
            alignment: Alignment.centerLeft,
            // ข้อความหัวข้อ
            child: Text(
              // ข้อความ เลือกชิ้นส่วนที่ต้องการปรับสี
              'เลือกชิ้นส่วนที่ต้องการปรับสี',
              // สไตล์ titleSmall ตัวหนา
              style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
            ),
          ),
        ),
        // ช่องว่าง 10
        const SizedBox(height: 10),
        // แถวเลือกชิ้นส่วน
        _PartSelectorRow(
          // รายการชิ้นส่วน
          parts: parts,
          // ชิ้นส่วนที่เลือกอยู่
          selectedPartId: state.selectedPartId,
          // เมื่อเลือกชิ้นส่วนใหม่ เรียก selectPart ใน state
          onPartSelected: state.selectPart,
        ),
        // ช่องว่าง 20
        const SizedBox(height: 20),
        // หัวข้อ เลือกสี พร้อมตัวอย่างสีและรหัสสี
        _ColorHeader(title: 'เลือกสี', color: selectedColor),
        // ช่องว่าง 10
        const SizedBox(height: 10),
        // ระยะขอบแนวนอน 20
        Padding(
          // ระยะขอบ
          padding: const EdgeInsets.symmetric(horizontal: 20),
          // วงล้อสี HueRingPicker
          child: HueRingPicker(
            // สีที่แสดงบนวงล้อ
            pickerColor: selectedColor,
            // ความสูงตัวเลือก 205
            colorPickerHeight: 205,
            // ความหนาแหวนสี 22
            hueRingStrokeWidth: 22,
            // ปิดช่องความโปร่งใส
            enableAlpha: false,
            // แสดงสีบนปุ่มเลื่อน
            displayThumbColor: true,
            // เมื่อผู้ใช้เปลี่ยนสี
            onColorChanged: (color) {
              // จำ id ชิ้นส่วนที่เลือกก่อน
              final partId = state.selectedPartId;
              // บันทึกสีลง state
              state.selectColorForCurrentPart(color);
              // ส่งสีไปแสดงบนโมเดล 3D ทันที
              _viewerKey.currentState?.applyColor(partId, color);
            },
          ),
        ),
      ],
    );
  }

  // สร้างแผงสติกเกอร์
  Widget _buildStickerPanel(BuildContext context, CustomizerState state) {
    // ตรวจว่ามีสติกเกอร์ที่เลือกอยู่
    final hasSelection = _selectedStickerId != null &&
        // และสติกเกอร์นั้นยังอยู่ในรายการจริง
        state.stickers.any((sticker) => sticker.id == _selectedStickerId);

    // ระยะขอบแผง
    return Padding(
      // key ใช้แยกแผงตอนสลับ
      key: const ValueKey('sticker-panel'),
      // ระยะขอบแนวนอน 20
      padding: const EdgeInsets.symmetric(horizontal: 20),
      // คอลัมน์ของแผง
      child: Column(
        // รายการลูก
        children: [
          // กล่องคำแนะนำ
          Container(
            // กว้างเต็ม
            width: double.infinity,
            // ระยะขอบใน 16
            padding: const EdgeInsets.all(16),
            // ตกแต่ง
            decoration: BoxDecoration(
              // พื้นขาว
              color: AppColors.surface,
              // ขอบมน 20
              borderRadius: BorderRadius.circular(20),
              // ขอบม่วงจาง ๆ
              border: Border.all(color: AppColors.primary.withOpacity(0.15)),
            ),
            // แถวไอคอนกับข้อความ
            child: const Row(
              // รายการลูก
              children: [
                // ไอคอนแตะ
                Icon(Icons.touch_app_outlined),
                // ช่องว่าง 12
                SizedBox(width: 12),
                // Expanded ให้ข้อความกินพื้นที่
                Expanded(
                  // ข้อความอธิบายวิธีวางและย้ายสติกเกอร์
                  child: Text('นำเข้ารูปแล้วแตะบนรองเท้าเพื่อวาง • ลากสติกเกอร์เพื่อย้ายบนผิวรองเท้า'),
                ),
              ],
            ),
          ),
          // ช่องว่าง 14
          const SizedBox(height: 14),
          // กล่องกำหนดขนาดปุ่ม
          SizedBox(
            // กว้างเต็ม
            width: double.infinity,
            // ปุ่มมีไอคอน
            child: FilledButton.icon(
              // เมื่อกดเรียก _importSticker
              onPressed: _importSticker,
              // ไอคอนเพิ่มรูป
              icon: const Icon(Icons.add_photo_alternate_outlined),
              // ข้อความ นำเข้ารูปเป็นสติกเกอร์
              label: const Text('นำเข้ารูปเป็นสติกเกอร์'),
              // ปรับสไตล์
              style: FilledButton.styleFrom(
                // พื้นสีม่วงหลัก
                backgroundColor: AppColors.primary,
                // ระยะขอบบนล่าง 14
                padding: const EdgeInsets.symmetric(vertical: 14),
              ),
            ),
          ),
          // ถ้ามีสติกเกอร์ที่รอวาง
          if (_pendingStickerId != null) ...[
            // ช่องว่าง 10
            const SizedBox(height: 10),
            // แถวข้อความแจ้ง
            const Row(
              // รายการลูก
              children: [
                // ไอคอนแตะ สีม่วงหลัก
                Icon(Icons.ads_click_rounded, color: AppColors.primary),
                // ช่องว่าง 8
                SizedBox(width: 8),
                // ข้อความ พร้อมวางแล้ว แตะบนตัวรองเท้า
                Expanded(child: Text('พร้อมวางแล้ว — แตะบนตัวรองเท้า')),
              ],
            ),
          ],
          // ถ้ามีสติกเกอร์บนรองเท้าอย่างน้อยหนึ่งชิ้น
          if (state.stickers.isNotEmpty) ...[
            // ช่องว่าง 12
            const SizedBox(height: 12),
            // แถวปุ่มทำสำเนาและลบ
            Row(
              // รายการลูก
              children: [
                // Expanded แบ่งพื้นที่เท่ากัน
                Expanded(
                  // ปุ่มทำสำเนา
                  child: OutlinedButton.icon(
                    // ถ้ามีสติกเกอร์ที่เลือกอยู่ ให้กดได้ ไม่เช่นนั้น null (ปุ่มจะเป็นสีเทา)
                    onPressed: hasSelection
                        // เมื่อกด
                        ? () {
                            // หาสติกเกอร์ที่เลือกอยู่
                            final source = state.stickers.firstWhere(
                              // เงื่อนไข id ตรงกับที่เลือก
                              (sticker) => sticker.id == _selectedStickerId,
                            );
                            // เตรียมสติกเกอร์ใหม่จากรูปเดิม (ทำสำเนา)
                            _prepareSticker(source.imageBytes);
                          }
                        // ถ้าไม่มีการเลือก ปุ่มกดไม่ได้
                        : null,
                    // ไอคอนสำเนา
                    icon: const Icon(Icons.copy_rounded),
                    // ข้อความ ทำสำเนา
                    label: const Text('ทำสำเนา'),
                  ),
                ),
                // ช่องว่าง 10
                const SizedBox(width: 10),
                // Expanded แบ่งพื้นที่เท่ากัน
                Expanded(
                  // ปุ่มลบ
                  child: OutlinedButton.icon(
                    // ถ้ามีสติกเกอร์ที่เลือกอยู่ ให้กดได้
                    onPressed: hasSelection
                        // เมื่อกด
                        ? () {
                            // สั่ง WebView ลบสติกเกอร์ออกจาก texture
                            _viewerKey.currentState?.removeSticker(_selectedStickerId!);
                            // ลบสติกเกอร์ออกจาก state
                            state.removeSticker(_selectedStickerId!);
                            // ยกเลิกการเลือก
                            setState(() => _selectedStickerId = null);
                          }
                        // ถ้าไม่มีการเลือก ปุ่มกดไม่ได้
                        : null,
                    // ไอคอนถังขยะ
                    icon: const Icon(Icons.delete_outline_rounded),
                    // ข้อความ ลบ
                    label: const Text('ลบ'),
                    // สีแดงเตือน
                    style: OutlinedButton.styleFrom(foregroundColor: Colors.redAccent),
                  ),
                ),
              ],
            ),
            // ช่องว่าง 8
            const SizedBox(height: 8),
            // ข้อความสถานะ
            Text(
              // ถ้ามีการเลือก
              hasSelection
                  // แสดงว่าเลือกแล้วพร้อมจำนวนทั้งหมด
                  ? 'เลือกสติกเกอร์แล้ว • ทั้งหมด ${state.stickers.length} ชิ้น'
                  // ไม่เช่นนั้นแนะนำให้แตะสติกเกอร์เพื่อเลือก
                  : 'แตะสติกเกอร์บนรองเท้าเพื่อเลือก • ทั้งหมด ${state.stickers.length} ชิ้น',
              // สไตล์ bodySmall สีเทา
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.textGray),
            ),
          ],
          // ถ้ามีสติกเกอร์ที่เลือกอยู่ แสดงแถบเลื่อนปรับขนาดและหมุน
          if (hasSelection) ...[
            // ช่องว่าง 12
            const SizedBox(height: 12),
            // Builder สร้างตัวแปรชั่วคราวภายในรายการ
            Builder(builder: (context) {
              // ดึงสติกเกอร์ที่เลือก
              final sticker = state.stickers.firstWhere(
                // เงื่อนไข id ตรงกัน
                (item) => item.id == _selectedStickerId,
              );
              // คืนคอลัมน์สองแถบเลื่อน
              return Column(
                // รายการลูก
                children: [
                  // แถวปรับขนาด
                  Row(
                    // รายการลูก
                    children: [
                      // ป้ายชื่อ ขนาด กว้าง 72
                      const SizedBox(width: 72, child: Text('ขนาด')),
                      // Expanded ให้แถบเลื่อนกินพื้นที่ที่เหลือ
                      Expanded(
                        // แถบเลื่อน
                        child: Slider(
                          // ค่าปัจจุบัน
                          value: sticker.scale,
                          // ค่าต่ำสุด 0.35 เท่า
                          min: 0.35,
                          // ค่าสูงสุด 3.2 เท่า
                          max: 3.2,
                          // เมื่อเลื่อน อัปเดตขนาดสติกเกอร์
                          onChanged: (value) => _updateSelectedSticker(state, scale: value),
                        ),
                      ),
                    ],
                  ),
                  // แถวปรับมุมหมุน
                  Row(
                    // รายการลูก
                    children: [
                      // ป้ายชื่อ หมุน กว้าง 72
                      const SizedBox(width: 72, child: Text('หมุน')),
                      // Expanded ให้แถบเลื่อนกินพื้นที่ที่เหลือ
                      Expanded(
                        // แถบเลื่อน
                        child: Slider(
                          // ค่าปัจจุบัน
                          value: sticker.rotation,
                          // ต่ำสุด ลบพาย เรเดียน (ประมาณ ลบ 180 องศา)
                          min: -3.14159,
                          // สูงสุด บวกพาย เรเดียน
                          max: 3.14159,
                          // เมื่อเลื่อน อัปเดตมุมหมุน
                          onChanged: (value) => _updateSelectedSticker(state, rotation: value),
                        ),
                      ),
                    ],
                  ),
                ],
              );
            }),
          ],
        ],
      ),
    );
  }

  // สร้างแผงวาดด้วยปากกา
  Widget _buildDrawingPanel(BuildContext context, CustomizerState state) {
    // ระยะขอบแผง
    return Padding(
      // key ใช้แยกแผงตอนสลับ
      key: const ValueKey('drawing-panel'),
      // ระยะขอบแนวนอน 20
      padding: const EdgeInsets.symmetric(horizontal: 20),
      // คอลัมน์ของแผง
      child: Column(
        // จัดชิดซ้าย
        crossAxisAlignment: CrossAxisAlignment.start,
        // รายการลูก
        children: [
          // หัวข้อ สีปากกา พร้อมตัวอย่างสี
          _ColorHeader(title: 'สีปากกา', color: state.brushColor, horizontalPadding: 0),
          // ช่องว่าง 8
          const SizedBox(height: 8),
          // จัดวงล้อสีให้อยู่กลาง
          Center(
            // วงล้อสี
            child: HueRingPicker(
              // สีที่แสดง คือสีปากกาปัจจุบัน
              pickerColor: state.brushColor,
              // ความสูง 180
              colorPickerHeight: 180,
              // ความหนาแหวน 20
              hueRingStrokeWidth: 20,
              // ปิดช่องโปร่งใส
              enableAlpha: false,
              // แสดงสีบนปุ่มเลื่อน
              displayThumbColor: true,
              // เมื่อเปลี่ยนสี
              onColorChanged: (color) {
                // บันทึกสีปากกาลง state
                state.setBrushColor(color);
                // ส่งสีไปยัง WebView
                _viewerKey.currentState?.setBrush(color: color);
              },
            ),
          ),
          // ช่องว่าง 12
          const SizedBox(height: 12),
          // หัวข้อ หัวปากกา
          Text('หัวปากกา', style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
          // ช่องว่าง 10
          const SizedBox(height: 10),
          // ให้แถวชิปเลื่อนแนวนอนได้
          SingleChildScrollView(
            // ทิศทางเลื่อนแนวนอน
            scrollDirection: Axis.horizontal,
            // แถวชิป
            child: Row(
              // รายการลูก
              children: [
                // ชิปหัวกลม
                _BrushChip(type: BrushType.round, label: 'กลม', icon: Icons.circle, state: state, onChanged: _setBrushType),
                // ชิปมาร์กเกอร์
                _BrushChip(type: BrushType.marker, label: 'มาร์กเกอร์', icon: Icons.edit, state: state, onChanged: _setBrushType),
                // ชิปไฮไลต์
                _BrushChip(type: BrushType.highlighter, label: 'ไฮไลต์', icon: Icons.border_color, state: state, onChanged: _setBrushType),
                // ชิปสเปรย์
                _BrushChip(type: BrushType.spray, label: 'สเปรย์', icon: Icons.blur_on, state: state, onChanged: _setBrushType),
              ],
            ),
          ),
          // ช่องว่าง 14
          const SizedBox(height: 14),
          // แถวปรับขนาดปากกา
          Row(
            // รายการลูก
            children: [
              // ไอคอนความหนาเส้น
              const Icon(Icons.line_weight_rounded, size: 20),
              // ช่องว่าง 8
              const SizedBox(width: 8),
              // ป้ายข้อความ ขนาด
              const Text('ขนาด'),
              // Expanded ให้แถบเลื่อนกินพื้นที่
              Expanded(
                // แถบเลื่อนขนาดปากกา
                child: Slider(
                  // ค่าปัจจุบัน
                  value: state.brushSize,
                  // ต่ำสุด 2
                  min: 2,
                  // สูงสุด 28
                  max: 28,
                  // แบ่งเป็น 26 ช่วง
                  divisions: 26,
                  // ป้ายตัวเลขที่แสดงตอนเลื่อน
                  label: state.brushSize.round().toString(),
                  // เมื่อเลื่อน
                  onChanged: (value) {
                    // บันทึกขนาดลง state
                    state.setBrushSize(value);
                    // ส่งขนาดไปยัง WebView
                    _viewerKey.currentState?.setBrush(size: value);
                  },
                ),
              ),
              // กล่องตัวอย่างขนาดปากกา
              SizedBox(
                // กว้าง 30
                width: 30,
                // สูง 30
                height: 30,
                // จัดกึ่งกลาง
                child: Center(
                  // วงกลมตัวอย่าง
                  child: Container(
                    // เส้นผ่านศูนย์กลางตามขนาดปากกา จำกัดช่วง 4 ถึง 26 (กว้าง)
                    width: state.brushSize.clamp(4, 26).toDouble(),
                    // เช่นเดียวกัน (สูง)
                    height: state.brushSize.clamp(4, 26).toDouble(),
                    // สีตามสีปากกา รูปวงกลม
                    decoration: BoxDecoration(color: state.brushColor, shape: BoxShape.circle),
                  ),
                ),
              ),
            ],
          ),
          // แถวปุ่มย้อนกลับและล้าง
          Row(
            // รายการลูก
            children: [
              // Expanded แบ่งพื้นที่เท่ากัน
              Expanded(
                // ปุ่มย้อนกลับ
                child: OutlinedButton.icon(
                  // ถ้าไม่มีเส้น ปุ่มกดไม่ได้
                  onPressed: state.drawingStrokes.isEmpty
                      // (ค่า null)
                      ? null
                      // ไม่เช่นนั้นเมื่อกด
                      : () {
                          // ลบเส้นล่าสุดออกจาก state และได้ id กลับมา
                          final id = state.undoLastStroke();
                          // ถ้ามี id สั่ง WebView ลบเส้นนั้นออกจาก texture
                          if (id != null) _viewerKey.currentState?.removeStroke(id);
                        },
                  // ไอคอนย้อนกลับ
                  icon: const Icon(Icons.undo_rounded),
                  // ข้อความ ย้อนกลับ
                  label: const Text('ย้อนกลับ'),
                ),
              ),
              // ช่องว่าง 10
              const SizedBox(width: 10),
              // Expanded แบ่งพื้นที่เท่ากัน
              Expanded(
                // ปุ่มล้างลายวาด
                child: OutlinedButton.icon(
                  // ถ้าไม่มีเส้น ปุ่มกดไม่ได้
                  onPressed: state.drawingStrokes.isEmpty
                      // (ค่า null)
                      ? null
                      // ไม่เช่นนั้นเมื่อกด
                      : () {
                          // ล้างเส้นทั้งหมดใน state
                          state.clearDrawing();
                          // สั่ง WebView ล้างลายวาดออกจาก texture
                          _viewerKey.currentState?.clearDrawing();
                        },
                  // ไอคอนกวาด
                  icon: const Icon(Icons.delete_sweep_outlined),
                  // ข้อความ ล้างลายวาด
                  label: const Text('ล้างลายวาด'),
                  // สีแดงเตือน
                  style: OutlinedButton.styleFrom(foregroundColor: Colors.redAccent),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // เปลี่ยนชนิดหัวปากกา
  void _setBrushType(BrushType type) {
    // บันทึกลง state
    context.read<CustomizerState>().setBrushType(type);
    // ส่งไปยัง WebView
    _viewerKey.currentState?.setBrush(type: type);
  }
}

// แถบเลือกเครื่องมือ เป็น StatelessWidget
class _ToolSelector extends StatelessWidget {
  // เครื่องมือที่เลือกอยู่
  final CustomizerTool selectedTool;
  // callback เมื่อเลือกเครื่องมือใหม่
  final ValueChanged<CustomizerTool> onSelected;

  // constructor ต้องระบุทั้งสองค่า
  const _ToolSelector({required this.selectedTool, required this.onSelected});

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // ระยะขอบแนวนอน 20
    return Padding(
      // ระยะขอบ
      padding: const EdgeInsets.symmetric(horizontal: 20),
      // SegmentedButton ปุ่มเลือกหนึ่งในสามแบบแคปซูล
      child: SegmentedButton<CustomizerTool>(
        // รายการตัวเลือก
        segments: const [
          // ตัวเลือก สี
          ButtonSegment(value: CustomizerTool.color, icon: Icon(Icons.palette_outlined), label: Text('สี')),
          // ตัวเลือก สติกเกอร์
          ButtonSegment(value: CustomizerTool.sticker, icon: Icon(Icons.photo_library_outlined), label: Text('สติกเกอร์')),
          // ตัวเลือก ปากกา
          ButtonSegment(value: CustomizerTool.draw, icon: Icon(Icons.brush_outlined), label: Text('ปากกา')),
        ],
        // ชุดที่เลือกอยู่ (เป็น Set จึงใส่ในปีกกา)
        selected: {selectedTool},
        // ซ่อนเครื่องหมายถูก
        showSelectedIcon: false,
        // เมื่อเลือกใหม่ ส่งค่าตัวแรกของชุดออกไป
        onSelectionChanged: (selection) => onSelected(selection.first),
        // ปรับให้ปุ่มกะทัดรัด
        style: const ButtonStyle(visualDensity: VisualDensity.compact),
      ),
    );
  }
}

// ป้ายบอกโหมดมุมขวาบนของโมเดล
class _ModeHint extends StatelessWidget {
  // เครื่องมือปัจจุบัน
  final CustomizerTool tool;
  // หมุนกล้องได้หรือไม่
  final bool canRotateModel;

  // constructor ต้องระบุทั้งสองค่า
  const _ModeHint({required this.tool, required this.canRotateModel});

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // เลือกคู่ของไอคอนและข้อความด้วย switch expression
    final (icon, label) = switch (tool) {
      // โหมดสีแต่ล็อกมุมมองอยู่ แสดงกุญแจ
      CustomizerTool.color when !canRotateModel => (Icons.lock_outline_rounded, 'ล็อกมุมมองแล้ว'),
      // โหมดสีปกติ แสดงลากเพื่อหมุน
      CustomizerTool.color => (Icons.threesixty, 'ลากเพื่อหมุน'),
      // โหมดสติกเกอร์ แสดงลากสติกเกอร์
      CustomizerTool.sticker => (Icons.open_with_rounded, 'ลากสติกเกอร์'),
      // โหมดวาด แสดงวาดบนรองเท้า
      CustomizerTool.draw => (Icons.brush_outlined, 'วาดบนรองเท้า'),
    };
    // Container ป้าย
    return Container(
      // ระยะขอบใน ซ้ายขวา 10 บนล่าง 6
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      // ตกแต่ง
      decoration: BoxDecoration(
        // พื้นดำโปร่งใส 42 เปอร์เซ็นต์
        color: Colors.black.withOpacity(0.42),
        // ขอบมนเต็ม แคปซูล
        borderRadius: BorderRadius.circular(999),
      ),
      // แถวไอคอนกับข้อความ
      child: Row(
        // ให้แถวกว้างเท่าเนื้อหา
        mainAxisSize: MainAxisSize.min,
        // รายการลูก
        children: [
          // ไอคอนสีขาว ขนาด 14
          Icon(icon, size: 14, color: Colors.white),
          // ช่องว่าง 4
          const SizedBox(width: 4),
          // ข้อความสีขาว ขนาด 11
          Text(label, style: const TextStyle(color: Colors.white, fontSize: 11)),
        ],
      ),
    );
  }
}

// ส่วนหัวของแผง แสดงหัวข้อ ตัวอย่างสี และรหัสสี
class _ColorHeader extends StatelessWidget {
  // หัวข้อ
  final String title;
  // สีที่แสดง
  final Color color;
  // ระยะขอบแนวนอน
  final double horizontalPadding;

  // constructor โดย horizontalPadding ค่าเริ่มต้น 20
  const _ColorHeader({required this.title, required this.color, this.horizontalPadding = 20});

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // ระยะขอบแนวนอน
    return Padding(
      // ระยะขอบ
      padding: EdgeInsets.symmetric(horizontal: horizontalPadding),
      // แถวเรียงหัวข้อ ตัวอย่างสี รหัสสี
      child: Row(
        // รายการลูก
        children: [
          // หัวข้อ
          Text(title, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
          // Spacer ดันส่วนที่เหลือไปชิดขวา
          const Spacer(),
          // วงกลมตัวอย่างสี
          Container(
            // กว้าง 22
            width: 22,
            // สูง 22
            height: 22,
            // ตกแต่ง
            decoration: BoxDecoration(
              // สีที่เลือก
              color: color,
              // รูปวงกลม
              shape: BoxShape.circle,
              // ขอบดำจาง ๆ
              border: Border.all(color: Colors.black.withOpacity(0.1)),
            ),
          ),
          // ช่องว่าง 8
          const SizedBox(width: 8),
          // ข้อความรหัสสี
          Text(
            // แปลงค่าสีเป็นฐานสิบหก ตัด 2 หลักแรก (อัลฟา) ทิ้ง แล้วทำเป็นตัวพิมพ์ใหญ่
            '#${color.value.toRadixString(16).substring(2).toUpperCase()}',
            // สไตล์ bodySmall
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  // สีเทา
                  color: AppColors.textGray,
                  // ใช้ตัวเลขกว้างเท่ากันทุกตัว เพื่อให้รหัสสีไม่ขยับเวลาเปลี่ยน
                  fontFeatures: const [FontFeature.tabularFigures()],
                ),
          ),
        ],
      ),
    );
  }
}

// ชิปเลือกหัวปากกาหนึ่งอัน
class _BrushChip extends StatelessWidget {
  // ชนิดหัวปากกาของชิปนี้
  final BrushType type;
  // ข้อความบนชิป
  final String label;
  // ไอคอนบนชิป
  final IconData icon;
  // state เพื่อดูว่าชิปนี้ถูกเลือกอยู่หรือไม่
  final CustomizerState state;
  // callback เมื่อเลือกชิป
  final ValueChanged<BrushType> onChanged;

  // constructor
  const _BrushChip({
    // type ต้องระบุ
    required this.type,
    // label ต้องระบุ
    required this.label,
    // icon ต้องระบุ
    required this.icon,
    // state ต้องระบุ
    required this.state,
    // onChanged ต้องระบุ
    required this.onChanged,
  });

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // ชิปนี้ถูกเลือกอยู่หรือไม่ (ชนิดตรงกับของ state)
    final selected = state.brushType == type;
    // ระยะขอบ
    return Padding(
      // ระยะห่างขวา 8
      padding: const EdgeInsets.only(right: 8),
      // ChoiceChip ชิปแบบเลือกได้
      child: ChoiceChip(
        // สถานะถูกเลือก
        selected: selected,
        // เมื่อเลือก เรียก onChanged ส่งชนิดนี้
        onSelected: (_) => onChanged(type),
        // ไอคอนสีขาวถ้าเลือก ไม่เช่นนั้นสีเข้ม
        avatar: Icon(icon, size: 18, color: selected ? Colors.white : AppColors.textDark),
        // ข้อความบนชิป
        label: Text(label),
        // สีตัวอักษรขาวถ้าเลือก
        labelStyle: TextStyle(color: selected ? Colors.white : AppColors.textDark),
        // สีพื้นเมื่อเลือกเป็นม่วงหลัก
        selectedColor: AppColors.primary,
      ),
    );
  }
}

// แถวเลือกชิ้นส่วนของรองเท้า
class _PartSelectorRow extends StatelessWidget {
  // รายการชิ้นส่วน
  final List<ShoePart3D> parts;
  // id ชิ้นส่วนที่เลือก
  final String selectedPartId;
  // callback เมื่อเลือกชิ้นส่วน
  final ValueChanged<String> onPartSelected;

  // constructor
  const _PartSelectorRow({
    // parts ต้องระบุ
    required this.parts,
    // selectedPartId ต้องระบุ
    required this.selectedPartId,
    // onPartSelected ต้องระบุ
    required this.onPartSelected,
  });

  // เขียนทับ build
  @override
  // build คืนหน้าตา
  Widget build(BuildContext context) {
    // กล่องกำหนดความสูง
    return SizedBox(
      // สูง 44
      height: 44,
      // ลิสต์แนวนอนพร้อมตัวคั่น
      child: ListView.separated(
        // เลื่อนแนวนอน
        scrollDirection: Axis.horizontal,
        // ระยะขอบซ้ายขวา 20
        padding: const EdgeInsets.symmetric(horizontal: 20),
        // จำนวนรายการเท่าจำนวนชิ้นส่วน
        itemCount: parts.length,
        // ตัวคั่นเป็นช่องว่างกว้าง 10
        separatorBuilder: (_, __) => const SizedBox(width: 10),
        // ฟังก์ชันสร้างแต่ละรายการ
        itemBuilder: (context, index) {
          // ชิ้นส่วนที่ index
          final part = parts[index];
          // ถูกเลือกอยู่หรือไม่
          final selected = part.id == selectedPartId;
          // GestureDetector จับการแตะ
          return GestureDetector(
            // เมื่อแตะ เรียก onPartSelected ส่ง id
            onTap: () => onPartSelected(part.id),
            // AnimatedContainer เปลี่ยนสไตล์แบบนุ่มนวล
            child: AnimatedContainer(
              // ใช้เวลา 200 มิลลิวินาที
              duration: const Duration(milliseconds: 200),
              // ระยะขอบใน ซ้ายขวา 18 บนล่าง 10
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
              // ตกแต่ง
              decoration: BoxDecoration(
                // ถ้าเลือกใช้ไล่สีม่วง ไม่เช่นนั้นไม่มี
                gradient: selected ? AppColors.primaryGradient : null,
                // ถ้าไม่ได้เลือกพื้นขาว
                color: selected ? null : AppColors.surface,
                // ขอบมนแคปซูล
                borderRadius: BorderRadius.circular(999),
                // ขอบ
                border: Border.all(
                  // โปร่งใสถ้าเลือก ม่วงจางถ้าไม่เลือก
                  color: selected ? Colors.transparent : AppColors.primary.withOpacity(0.2),
                ),
                // เงาเฉพาะตอนถูกเลือก
                boxShadow: selected
                    // เงาหนึ่งชั้น
                    ? [
                        // ม่วงโปร่งใส 30 เปอร์เซ็นต์
                        BoxShadow(
                          // เบลอ 12
                          color: AppColors.primary.withOpacity(0.3),
                          // เลื่อนลง 6
                          blurRadius: 12,
                          offset: const Offset(0, 6),
                        ),
                      ]
                    // ไม่เลือกก็ไม่มีเงา
                    : null,
              ),
              // จัดข้อความกึ่งกลาง
              alignment: Alignment.center,
              // ข้อความชื่อชิ้นส่วน
              child: Text(
                // ชื่อชิ้นส่วน
                part.label,
                // สไตล์ตัวอักษร
                style: TextStyle(
                  // สีขาวถ้าเลือก
                  color: selected ? Colors.white : AppColors.textDark,
                  // ตัวหนา w600
                  fontWeight: FontWeight.w600,
                  // ขนาด 13
                  fontSize: 13,
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}
