// นำเข้า dart:async สำหรับ Timer, Future และ unawaited
import 'dart:async';
// นำเข้า dart:convert สำหรับ jsonEncode, jsonDecode, base64Encode, utf8
import 'dart:convert';
// นำเข้า dart:typed_data สำหรับ Uint8List (ไบต์รูป)
import 'dart:typed_data';

// นำเข้า Material
import 'package:flutter/material.dart';
// นำเข้า rootBundle เพื่ออ่านไฟล์ asset (เช่น ไฟล์ JavaScript) เป็นข้อความ
import 'package:flutter/services.dart' show rootBundle;
// นำเข้า ModelViewer จากแพ็กเกจ model_viewer_plus (แสดงไฟล์ GLB ผ่าน WebView)
import 'package:model_viewer_plus/model_viewer_plus.dart';
// นำเข้าเฉพาะคลาสที่ต้องใช้จาก webview_flutter
import 'package:webview_flutter/webview_flutter.dart'
    show JavascriptChannel, WebViewController;

// นำเข้าโมเดลสติกเกอร์/เส้นวาด/enum เครื่องมือ
import '../models/custom_design.dart';
// นำเข้าข้อมูลชิ้นส่วนรองเท้า
import '../models/shoe_parts.dart';

// ชนิดฟังก์ชัน callback รับ id และตำแหน่งผิวของสติกเกอร์
typedef StickerSurfaceCallback = void Function(String id, String surface);
// ชนิดฟังก์ชัน callback รับ id เส้นและตำแหน่งผิวของจุดวาด
typedef StrokeSurfaceCallback = void Function(String strokeId, String surface);

/// แสดงรองเท้า 3D และวาดสติกเกอร์/ลายเส้นลง texture ผ่านพิกัด UV ของ mesh
// วิดเจ็ตหลักแสดงรองเท้า 3D เป็น StatefulWidget เพราะมี WebView และตัวจับเวลา
class ShoeModelViewer extends StatefulWidget {
  // พาธไฟล์ GLB ที่จะแสดง
  final String assetPath;
  // รายการชิ้นส่วนที่ปรับสีได้ของโมเดลนี้
  final List<ShoePart3D> parts;
  // สีเริ่มต้นของแต่ละชิ้นส่วน (partId ไปยังสี)
  final Map<String, Color> initialColors;
  // ให้โมเดลหมุนเองหรือไม่
  final bool autoRotate;
  // ให้ผู้ใช้หมุนกล้องด้วยนิ้วหรือไม่
  final bool cameraControls;
  // เปิดโหมดแก้ไขลวดลาย (สติกเกอร์/วาด) หรือไม่
  final bool decorationEditable;
  // เครื่องมือที่เลือกอยู่ (สี สติกเกอร์ วาด)
  final CustomizerTool tool;
  // สีปากกา
  final Color brushColor;
  // ขนาดปากกา
  final double brushSize;
  // ชนิดหัวปากกา
  final BrushType brushType;
  // รายการสติกเกอร์ทั้งหมดที่ต้องแสดง
  final List<ShoeSticker> stickers;
  // รายการเส้นที่วาดทั้งหมดที่ต้องแสดง
  final List<DrawingStroke> drawingStrokes;
  // id สติกเกอร์ที่ถูกเลือกอยู่
  final String? selectedStickerId;
  // callback เมื่อวางสติกเกอร์ลงผิว
  final StickerSurfaceCallback? onStickerPlaced;
  // callback เมื่อย้ายสติกเกอร์
  final StickerSurfaceCallback? onStickerMoved;
  // callback เมื่อเลือกสติกเกอร์ (ส่ง null เมื่อยกเลิก)
  final ValueChanged<String?>? onStickerSelected;
  // callback เมื่อเริ่มลากเส้นใหม่
  final StrokeSurfaceCallback? onStrokeStarted;
  // callback เมื่อมีจุดใหม่ต่อท้ายเส้น
  final StrokeSurfaceCallback? onStrokePointAdded;

  // constructor แบบ const
  const ShoeModelViewer({
    // ส่งต่อ key
    super.key,
    // assetPath ต้องระบุ
    required this.assetPath,
    // parts ต้องระบุ
    required this.parts,
    // initialColors ต้องระบุ
    required this.initialColors,
    // autoRotate ค่าเริ่มต้น false
    this.autoRotate = false,
    // cameraControls ค่าเริ่มต้น true
    this.cameraControls = true,
    // decorationEditable ค่าเริ่มต้น false (หน้าสรุปแค่ดู)
    this.decorationEditable = false,
    // tool ค่าเริ่มต้นเป็นเครื่องมือเปลี่ยนสี
    this.tool = CustomizerTool.color,
    // brushColor ค่าเริ่มต้นดำ
    this.brushColor = Colors.black,
    // brushSize ค่าเริ่มต้น 7
    this.brushSize = 7,
    // brushType ค่าเริ่มต้นหัวกลม
    this.brushType = BrushType.round,
    // stickers ค่าเริ่มต้นลิสต์ว่าง
    this.stickers = const [],
    // drawingStrokes ค่าเริ่มต้นลิสต์ว่าง
    this.drawingStrokes = const [],
    // selectedStickerId ไม่บังคับ
    this.selectedStickerId,
    // onStickerPlaced ไม่บังคับ
    this.onStickerPlaced,
    // onStickerMoved ไม่บังคับ
    this.onStickerMoved,
    // onStickerSelected ไม่บังคับ
    this.onStickerSelected,
    // onStrokeStarted ไม่บังคับ
    this.onStrokeStarted,
    // onStrokePointAdded ไม่บังคับ
    this.onStrokePointAdded,
  });

  // เขียนทับ createState
  @override
  // คืน State ที่เป็น public (ไม่มี _) เพื่อให้หน้าอื่นเรียกเมธอดผ่าน GlobalKey ได้
  State<ShoeModelViewer> createState() => ShoeModelViewerState();
}

// คลาส State ของตัวแสดงโมเดล
class ShoeModelViewerState extends State<ShoeModelViewer> {
  // ข้อความ JavaScript รวมที่จะฉีดเข้า WebView (null คือยังโหลดไม่เสร็จ)
  String? _painterScript;
  // ข้อความ error ถ้าโหลดไฟล์ asset ไม่สำเร็จ
  String? _assetError;
  // ตัวควบคุม WebView (ได้มาหลัง WebView ถูกสร้าง)
  WebViewController? _controller;
  // ตัวจับเวลาหน่วง 33 มิลลิวินาที สำหรับส่งสี
  Timer? _colorTimer;
  // ธง: กำลังส่งสีไปยัง WebView อยู่หรือไม่
  bool _sendingColors = false;
  // คิวสีที่รอส่ง (เก็บเฉพาะสีล่าสุดของแต่ละชิ้นส่วน)
  final Map<String, Color> _pendingColors = {};
  // สีปัจจุบันของทุกชิ้นส่วน
  late Map<String, Color> _colors;
  // เครื่องมือปัจจุบัน
  late CustomizerTool _tool;
  // สีปากกาปัจจุบัน
  late Color _brushColor;
  // ขนาดปากกาปัจจุบัน
  late double _brushSize;
  // ชนิดหัวปากกาปัจจุบัน
  late BrushType _brushType;
  // ข้อมูลสติกเกอร์ที่รอให้ผู้ใช้แตะวาง
  Map<String, Object?>? _pendingSticker;

  // เขียนทับ initState
  @override
  // เรียก initState ของคลาสแม่
  void initState() {
    super.initState();
    // คัดลอกสีเริ่มต้นเป็นแผนที่ของตัวเอง
    _colors = Map.of(widget.initialColors);
    // เก็บเครื่องมือเริ่มต้น
    _tool = widget.tool;
    // เก็บสีปากกาเริ่มต้น
    _brushColor = widget.brushColor;
    // เก็บขนาดปากกาเริ่มต้น
    _brushSize = widget.brushSize;
    // เก็บชนิดหัวปากกาเริ่มต้น
    _brushType = widget.brushType;
    // เริ่มโหลดไฟล์ asset แบบไม่รอ (unawaited บอกว่าตั้งใจไม่ await)
    unawaited(_loadPainterAssets());
  }

  // โหลดสคริปต์ที่ต้องฉีดเข้า WebView
  Future<void> _loadPainterAssets() async {
    // ใช้ try เพื่อดักข้อผิดพลาด
    try {
      // อ่านไฟล์ JavaScript ตัววาด texture (บันเดิลของ three.js และโค้ดตัววาด) ผ่าน rootBundle
      final engine = await rootBundle.loadString('assets/shoe_painter_v3_2.js');
      // อ่านไฟล์ตัวถอดรหัส Draco สำหรับโมเดลที่บีบอัด
      final decoder = await rootBundle.loadString('assets/draco/draco_decoder.js');
      // แปลงตัวถอดรหัสเป็น data URL แบบ base64 เพื่อให้ WebView โหลดได้โดยไม่ต้องขอไฟล์จากเซิร์ฟเวอร์
      final decoderUrl = 'data:application/javascript;base64,${base64Encode(utf8.encode(decoder))}';
      // ต่อสคริปต์: ตัววาด บวกบรรทัดกำหนดตัวแปร __shoeDecoderUrl
      final script = engine + '\nwindow.__shoeDecoderUrl = ' +
          // แล้วต่อด้วยโค้ดตัวเริ่มต้นตัววาด (loader)
          jsonEncode(decoderUrl) + ';\n' + _texturePainterLoaderJavaScript;
      // ถ้า widget ยังอยู่ ให้เก็บสคริปต์และสั่งวาดใหม่ (แล้ว build จะสร้าง ModelViewer)
      if (mounted) setState(() => _painterScript = script);
    // ถ้าเกิดข้อผิดพลาด
    } catch (error) {
      // เก็บข้อความ error เพื่อแสดงบนหน้าจอ
      if (mounted) setState(() => _assetError = 'โหลดไฟล์เครื่องมือไม่สำเร็จ: $error');
    }
  }

  // เขียนทับ didUpdateWidget เรียกเมื่อพ่อส่งค่าใหม่มา
  @override
  // เรียกของคลาสแม่
  void didUpdateWidget(covariant ShoeModelViewer oldWidget) {
    super.didUpdateWidget(oldWidget);
    // อัปเดตเครื่องมือจากค่าใหม่
    _tool = widget.tool;
    // อัปเดตสีปากกา
    _brushColor = widget.brushColor;
    // อัปเดตขนาดปากกา
    _brushSize = widget.brushSize;
    // อัปเดตชนิดหัวปากกา
    _brushType = widget.brushType;
    // ถ้า WebView พร้อมแล้ว
    if (_controller != null) {
      // ส่งค่า config ใหม่ไปยัง JavaScript
      _syncEditorConfig();
      // ส่งสติกเกอร์ที่เลือกอยู่ไปยัง JavaScript
      selectSticker(widget.selectedStickerId);
    }
  }

  // เมธอด public สำหรับเปลี่ยนสีชิ้นส่วน (เรียกจากหน้า customizer)
  Future<void> applyColor(String partId, Color color) async {
    // บันทึกสีล่าสุดของชิ้นส่วนนี้
    _colors[partId] = color;
    // ใส่ในคิวรอส่ง (ทับค่าเก่าของชิ้นส่วนเดียวกัน)
    _pendingColors[partId] = color;
    // นัดเวลาส่ง
    _scheduleColors();
  }

  // นัดส่งสีแบบ coalescing (รวมการเปลี่ยนสีถี่ ๆ ให้เหลือ 1 ครั้งต่อ 33 มิลลิวินาที)
  void _scheduleColors() {
    // ไม่นัดถ้า widget ถูกถอด ยังไม่มี controller กำลังส่งอยู่ มีตัวจับเวลาอยู่แล้ว หรือไม่มีสีรอส่ง
    if (!mounted || _controller == null || _sendingColors ||
        // (ต่อเงื่อนไขข้างบน) แล้ว return ออกไปเลย
        _colorTimer != null || _pendingColors.isEmpty) return;
    // ตั้งตัวจับเวลา 33 มิลลิวินาที
    _colorTimer = Timer(const Duration(milliseconds: 33), () {
      // เมื่อครบเวลา เคลียร์ตัวจับเวลา
      _colorTimer = null;
      // ส่งสีที่ค้างอยู่
      unawaited(_flushColors());
    });
  }

  // ส่งสีที่ค้างไปยัง WebView
  Future<void> _flushColors() async {
    // ถ้า widget ถูกถอดหรือกำลังส่งอยู่แล้ว ให้ออก
    if (!mounted || _sendingColors) return;
    // ตั้งธงว่ากำลังส่ง
    _sendingColors = true;
    // ถ่ายสำเนาคิวเป็น batch
    final batch = Map<String, Color>.of(_pendingColors);
    // ล้างคิวหลัก (สีใหม่ที่เข้ามาระหว่างส่งจะอยู่ในคิวรอบถัดไป)
    _pendingColors.clear();
    // ใช้ try เพื่อให้ finally ทำงานเสมอ
    try {
      // วนทุกคู่ชิ้นส่วนกับสีใน batch
      for (final entry in batch.entries) {
        // ถ้า widget ถูกถอดให้หยุด
        if (!mounted) break;
        // ส่งสีทีละชิ้นส่วน และรอให้เสร็จก่อนส่งชิ้นถัดไป (ส่งแบบอนุกรม)
        await _applySinglePartColor(entry.key, entry.value);
      }
    // ไม่ว่าสำเร็จหรือล้มเหลว
    } finally {
      // ปลดธงกำลังส่ง
      _sendingColors = false;
      // นัดส่งรอบถัดไปถ้ายังมีสีค้าง
      _scheduleColors();
    }
  }

  // เขียนทับ dispose
  @override
  // ยกเลิกตัวจับเวลาที่ค้าง
  void dispose() {
    // ล้างคิวสี
    _colorTimer?.cancel();
    // เรียก dispose ของคลาสแม่
    _pendingColors.clear();
    super.dispose();
  }

  // เปลี่ยนเครื่องมือ (สี สติกเกอร์ วาด)
  Future<void> setEditorTool(CustomizerTool tool) async {
    // เก็บเครื่องมือ
    _tool = tool;
    // ซิงก์ config ไปยัง JavaScript
    await _syncEditorConfig();
  }

  // ตั้งค่าปากกา ส่งเฉพาะค่าที่ต้องการเปลี่ยน
  Future<void> setBrush({Color? color, double? size, BrushType? type}) async {
    // ถ้ามีสีใหม่ก็เก็บ
    if (color != null) _brushColor = color;
    // ถ้ามีขนาดใหม่ก็เก็บ
    if (size != null) _brushSize = size;
    // ถ้ามีชนิดใหม่ก็เก็บ
    if (type != null) _brushType = type;
    // ซิงก์ config ไปยัง JavaScript
    await _syncEditorConfig();
  }

  /// เตรียมรูปไว้ แล้วให้ผู้ใช้แตะผิวรองเท้าเพื่อวางตำแหน่งจริง
  // เตรียมรูปสติกเกอร์ไว้ รอผู้ใช้แตะผิวรองเท้าเพื่อวาง
  Future<void> prepareSticker(String id, Uint8List bytes) async {
    // สร้างข้อมูลสติกเกอร์ที่รอวาง
    _pendingSticker = {
      // id ของสติกเกอร์
      'id': id,
      // ข้อมูลรูปในรูปแบบ data URL
      'src': _dataUrl(bytes),
      // ขนาดเริ่มต้น 1.0
      'scale': 1.0,
      // มุมหมุนเริ่มต้น 0.0
      'rotation': 0.0,
    };
    // ส่งไปยัง JavaScript เมธอด setPendingSticker
    await _runEditor('setPendingSticker', _pendingSticker);
  }

  // ยกเลิกสติกเกอร์ที่รอวาง
  Future<void> cancelPendingSticker() async {
    // ล้างข้อมูลที่รอวาง
    _pendingSticker = null;
    // แจ้ง JavaScript ให้ล้างด้วย
    await _runEditor('setPendingSticker', null);
  }

  // เลือกสติกเกอร์ตาม id (null คือยกเลิกการเลือก)
  Future<void> selectSticker(String? id) async {
    // ส่งคำสั่ง selectSticker ไปยัง JavaScript
    await _runEditor('selectSticker', id);
  }

  // อัปเดตตำแหน่ง ขนาด มุมหมุนของสติกเกอร์
  Future<void> updateStickerTransform(ShoeSticker sticker) async {
    // ส่งคำสั่ง updateSticker พร้อมข้อมูลทั้งหมด
    await _runEditor('updateSticker', {
      // id
      'id': sticker.id,
      // ตำแหน่งบนผิว
      'surface': sticker.surface,
      // ขนาด
      'scale': sticker.scale,
      // มุมหมุน
      'rotation': sticker.rotation,
    });
  }

  // ลบสติกเกอร์ออกจาก texture
  Future<void> removeSticker(String id) async {
    // ส่งคำสั่ง removeSticker
    await _runEditor('removeSticker', id);
  }

  // ลบเส้นหนึ่งเส้นออกจาก texture
  Future<void> removeStroke(String id) async {
    // ส่งคำสั่ง removeStroke
    await _runEditor('removeStroke', id);
  }

  // ล้างลายวาดทั้งหมดออกจาก texture
  Future<void> clearDrawing() async {
    // ส่งคำสั่ง clearDrawing
    await _runEditor('clearDrawing', null);
  }

  // ส่ง config ปัจจุบันไปให้ JavaScript
  Future<void> _syncEditorConfig() async {
    // เรียก setConfig
    await _runEditor('setConfig', {
      // ชื่อเครื่องมือ ถ้าแก้ไขลวดลายไม่ได้ให้บังคับเป็นโหมดสี
      'tool': widget.decorationEditable ? _tool.name : 'color',
      // สีปากกาแปลงเป็นรหัสฐานสิบหก
      'color': _colorHex(_brushColor),
      // ขนาดปากกา
      'size': _brushSize,
      // ชื่อชนิดหัวปากกา
      'brush': _brushType.name,
      // เปิดให้แก้ไขหรือไม่
      'editable': widget.decorationEditable,
    });
  }

  // ฟังก์ชันกลางสำหรับสั่งเมธอดของ window.shoePainter ใน WebView
  Future<void> _runEditor(String method, Object? argument) async {
    // ดึง controller ปัจจุบัน
    final controller = _controller;
    // ถ้ายังไม่มี WebView ก็ไม่ทำอะไร
    if (controller == null) return;
    // ใช้ try ดักข้อผิดพลาด
    try {
      // สั่งรันโค้ด JavaScript ใน WebView
      await controller.runJavaScript(
        // ถ้ามีตัวคิว callShoePainter ให้เรียกผ่านมัน (คำสั่งจะถูกเก็บคิวไว้ถ้าตัววาดยังไม่พร้อม)
        "if (window.callShoePainter) { window.callShoePainter('$method', [${jsonEncode(argument)}]); } "
        // ไม่เช่นนั้นถ้ามี shoePainter และเมธอดนั้นอยู่ ให้เรียกตรง ๆ
        "else if (window.shoePainter && window.shoePainter.$method) { window.shoePainter.$method(${jsonEncode(argument)}); }",
      );
    // ถ้า error (เช่น WebView ปิดอยู่)
    } catch (_) {
      // WebView อาจกำลังเปลี่ยนหน้า/ปิดตัวอยู่
    }
  }

  /// ส่ง state ปัจจุบันซ้ำเมื่อ JavaScript bridge พร้อม โดยไม่ต้องรอ ModelLoaded
  // วนลองจนกว่า JavaScript จะพร้อม แล้วส่งสถานะปัจจุบันทั้งหมดไป
  Future<void> _primePainterBridge(WebViewController controller) async {
    // ลองสูงสุด 120 ครั้ง
    for (var attempt = 0; attempt < 120; attempt++) {
      // ถ้า widget ถูกถอดหรือ controller เปลี่ยนไปแล้วให้เลิก
      if (!mounted || _controller != controller) return;
      // ใช้ try ดัก error ขณะหน้า HTML ยังไม่พร้อม
      try {
        // ถามค่า Boolean ใน WebView ว่ามี callShoePainter แล้วหรือยัง
        final result = await controller.runJavaScriptReturningResult(
          // นิพจน์ JavaScript ที่ถาม
          'Boolean(window.callShoePainter)',
        );
        // ถ้าพร้อมแล้ว (ผลเป็น true)
        if (result == true || result.toString() == 'true') {
          // ส่งสีทุกชิ้นส่วน
          await _applyAllColors();
          // วาดสติกเกอร์และเส้นที่เก็บไว้ลง texture
          await _renderStoredDecorations();
          // ส่ง config เครื่องมือ
          await _syncEditorConfig();
          // ส่งสติกเกอร์ที่เลือก
          await selectSticker(widget.selectedStickerId);
          // ถ้ามีสติกเกอร์รอวาง
          if (_pendingSticker != null) {
            // ส่งสติกเกอร์ที่รอวางไปด้วย
            await _runEditor('setPendingSticker', _pendingSticker);
          }
          // เสร็จงาน ออกจากฟังก์ชัน
          return;
        }
      // ถ้า error ในรอบนี้
      } catch (_) {
        // หน้า HTML อาจยังเริ่มทำงานไม่ครบ รอแล้วลองใหม่
      }
      // รอ 250 มิลลิวินาทีแล้วลองรอบถัดไป
      await Future<void>.delayed(const Duration(milliseconds: 250));
    }
  }

  // ส่งสีของทุกชิ้นส่วนไปยัง WebView
  Future<void> _applyAllColors() async {
    // วนทุกคู่ชิ้นส่วนกับสี
    for (final entry in _colors.entries) {
      // ส่งทีละชิ้นส่วน
      await _applySinglePartColor(entry.key, entry.value);
    }
  }

  // ส่งสีของชิ้นส่วนเดียวไปยัง WebView
  Future<void> _applySinglePartColor(String partId, Color color) async {
    // หาชิ้นส่วนจาก id
    final part = findShoePart(widget.parts, partId);
    // ดึง controller
    final controller = _controller;
    // ถ้าไม่พบชิ้นส่วนหรือยังไม่มี WebView ให้ออก
    if (part == null || controller == null) return;
    // ค่าสีแดงแปลงจาก 0 ถึง 255 เป็น 0 ถึง 1 (รูปแบบที่ three.js ใช้) ทศนิยม 4 ตำแหน่ง
    final r = (color.red / 255.0).toStringAsFixed(4);
    // ค่าสีเขียวแบบเดียวกัน
    final g = (color.green / 255.0).toStringAsFixed(4);
    // ค่าสีน้ำเงินแบบเดียวกัน
    final b = (color.blue / 255.0).toStringAsFixed(4);
    // รายการเลข material ของชิ้นส่วน ต่อกันด้วยจุลภาค
    final indicesJs = part.materialIndices.join(',');
    // สร้างโค้ด JavaScript เป็นข้อความ (ข้างในมีตัวแปรของ Dart แทรกอยู่)
    final js = '''
      // JS: ฟังก์ชันเรียกตัวเองทันทีเพื่อไม่ให้ตัวแปรหลุดออกไปข้างนอก
      (function() {
        // JS: รายการเลข material ที่จะเปลี่ยนสี
        var indices = [$indicesJs];
        // JS: ถ้ามีคิวคำสั่งของตัววาด
        if (window.callShoePainter) {
          // JS: ส่งคำสั่ง setMaterialColor พร้อมรายการ material และสี RGB
          window.callShoePainter('setMaterialColor', [indices, [$r, $g, $b]]);
          // JS: จบการทำงาน
          return;
        }
        // JS: ถ้าตัววาดพร้อมแล้ว (ไม่ผ่านคิว)
        if (window.shoePainter) {
          // JS: เรียก setMaterialColor ตรง ๆ
          window.shoePainter.setMaterialColor(indices, [$r, $g, $b]);
          // JS: จบการทำงาน
          return;
        }
        // JS: ทางสำรอง ค้นหาองค์ประกอบ model-viewer ในหน้า
        var mv = document.querySelector('model-viewer');
        // JS: ถ้าไม่พบหรือโมเดลยังไม่โหลดให้ออก
        if (!mv || !mv.model) return;
        // JS: วนทุก material ตามเลขที่กำหนด
        indices.forEach(function(i) {
          // JS: ดึง material ของโมเดลตามเลข
          var mat = mv.model.materials[i];
          // JS: ถ้ามี material และมีค่า PBR
          if (mat && mat.pbrMetallicRoughness) {
            // JS: ตั้งสีฐานของ material เป็นสีใหม่ (ช่องอัลฟา 1.0)
            mat.pbrMetallicRoughness.setBaseColorFactor([$r, $g, $b, 1.0]);
          }
        });
      })();
    ''';
    try {
      // สั่งรัน JavaScript ใน WebView
      await controller.runJavaScript(js);
    // ถ้า error
    } catch (_) {
      // เพิกเฉยหาก WebView ยังไม่พร้อม 100%
    }
  }

  // วาดสติกเกอร์และเส้นที่เก็บไว้กลับลงบน texture (ใช้ตอน WebView เพิ่งสร้างใหม่)
  Future<void> _renderStoredDecorations() async {
    // สร้างข้อมูลที่จะส่ง
    final payload = {
      // รายการสติกเกอร์แปลงเป็นข้อมูล JSON ผ่าน _stickerPayload
      'stickers': widget.stickers.map(_stickerPayload).toList(growable: false),
      // รายการเส้น แต่ละเส้นแปลงเป็นแผนที่
      'strokes': widget.drawingStrokes.map((stroke) => {
            // id เส้น
            'id': stroke.id,
            // รายการตำแหน่งผิวของแต่ละจุด
            'surfaces': stroke.surfacePoints,
            // สีเป็นรหัสฐานสิบหก
            'color': _colorHex(stroke.color),
            // ความหนาเส้น
            'size': stroke.width,
            // ชื่อชนิดหัวปากกา
            'brush': stroke.brushType.name,
          }).toList(growable: false),
    };
    // ส่งคำสั่ง renderAll ไปยัง JavaScript
    await _runEditor('renderAll', payload);
  }

  // แปลงสติกเกอร์หนึ่งชิ้นเป็นแผนที่สำหรับส่ง JSON
  Map<String, Object?> _stickerPayload(ShoeSticker sticker) => {
        // id
        'id': sticker.id,
        // รูปเป็น data URL
        'src': _dataUrl(sticker.imageBytes),
        // ตำแหน่งบนผิว
        'surface': sticker.surface,
        // ขนาด
        'scale': sticker.scale,
        // มุมหมุน
        'rotation': sticker.rotation,
      };

  // แปลงไบต์รูปเป็น data URL เพื่อให้ JavaScript ใช้เป็น src ของรูปได้
  String _dataUrl(Uint8List bytes) {
    // ค่าเริ่มต้นเป็น PNG
    var mime = 'image/png';
    // ถ้าไบต์ต้นฉบับเริ่มด้วย FF D8 แปลว่าเป็น JPEG
    if (bytes.length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8) {
      // ตั้งเป็น JPEG
      mime = 'image/jpeg';
    // ไม่เช่นนั้นถ้าไบต์เริ่มด้วยอักษร RIFF (52 49 46 46)
    } else if (bytes.length >= 12 &&
        bytes[0] == 0x52 && bytes[1] == 0x49 &&
        bytes[2] == 0x46 && bytes[3] == 0x46) {
      // ตั้งเป็น WebP (ตรวจแบบง่าย)
      mime = 'image/webp';
    }
    // คืน data URL ที่เข้ารหัสไบต์เป็น base64
    return 'data:$mime;base64,${base64Encode(bytes)}';
  }

  // แปลงสีเป็นรหัสฐานสิบหก 6 หลัก เช่น #ff00aa
  String _colorHex(Color color) =>
      // เอาค่าสี 8 หลัก (อัลฟานำหน้า) ตัด 2 หลักแรกทิ้ง
      '#${color.value.toRadixString(16).padLeft(8, '0').substring(2)}';

  // จัดการข้อความที่ JavaScript ส่งกลับมาทางช่อง DesignEvent
  void _handleDesignMessage(String rawMessage) {
    // ใช้ try ข้ามข้อความที่ไม่ใช่ JSON
    try {
      // แปลงข้อความ JSON เป็นแผนที่
      final event = jsonDecode(rawMessage) as Map<String, dynamic>;
      // ชนิดเหตุการณ์
      final type = event['type'] as String?;
      // id ที่เกี่ยวข้อง
      final id = event['id'] as String?;
      // ตำแหน่งบนผิว
      final surface = event['surface'] as String?;
      // แยกตามชนิดเหตุการณ์
      switch (type) {
        // สติกเกอร์ถูกวางแล้ว
        case 'stickerPlaced':
          // ถ้ามี id และตำแหน่งครบ
          if (id != null && surface != null) {
            // เคลียร์สติกเกอร์ที่รอวาง
            _pendingSticker = null;
            // เรียก callback แจ้งหน้าแม่
            widget.onStickerPlaced?.call(id, surface);
          }
          // จบ case
          break;
        // สติกเกอร์ถูกย้าย
        case 'stickerMoved':
          // ถ้าข้อมูลครบ เรียก callback
          if (id != null && surface != null) widget.onStickerMoved?.call(id, surface);
          // จบ case
          break;
        // สติกเกอร์ถูกเลือก
        case 'stickerSelected':
          // เรียก callback (id อาจเป็น null)
          widget.onStickerSelected?.call(id);
          // จบ case
          break;
        // เริ่มลากเส้นใหม่
        case 'strokeStarted':
          // ถ้าข้อมูลครบ เรียก callback
          if (id != null && surface != null) widget.onStrokeStarted?.call(id, surface);
          // จบ case
          break;
        // มีจุดใหม่ต่อท้ายเส้น
        case 'strokePoint':
          // ถ้าข้อมูลครบ เรียก callback
          if (id != null && surface != null) widget.onStrokePointAdded?.call(id, surface);
          // จบ case
          break;
      }
    // ถ้า error ในการแปลง
    } catch (_) {
      // ข้ามข้อความที่ไม่ใช่ event ของ editor
    }
  }

  // เขียนทับ build
  @override
  // ถ้าโหลด asset ผิดพลาด แสดงข้อความ error กลางจอ
  Widget build(BuildContext context) {
    // ถ้ายังโหลดสคริปต์ไม่เสร็จ แสดงตัวหมุนโหลดกลางจอ
    if (_assetError != null) return Center(child: Text(_assetError!));
    // (พร้อมแล้ว) สร้างตัวแสดงโมเดลผ่าน ModelViewer
    if (_painterScript == null) return const Center(child: CircularProgressIndicator());
    // key อิงพาธโมเดล เมื่อเปลี่ยนโมเดลจะสร้าง WebView ใหม่
    return ModelViewer(
      // พื้นหลังโปร่งใส
      key: ValueKey(widget.assetPath),
      // ไฟล์โมเดล GLB (เป็น asset ของแอป)
      backgroundColor: Colors.transparent,
      // ข้อความอธิบายสำหรับผู้พิการทางสายตา
      src: widget.assetPath,
      // หมุนเองหรือไม่ตามที่ส่งมา
      alt: 'โมเดล 3D รองเท้าที่กำลังออกแบบ',
      // เริ่มหมุนทันที (หน่วง 0)
      autoRotate: widget.autoRotate,
      // ความเร็วหมุน 18 องศาต่อวินาที
      autoRotateDelay: 0,
      // ให้ผู้ใช้หมุนกล้องหรือไม่
      rotationPerSecond: '18deg',
      // อนุญาตให้ซูม
      cameraControls: widget.cameraControls,
      // ปิด AR
      disableZoom: false,
      // เมื่อ WebView ถูกสร้าง
      ar: false,
      // เก็บ controller
      onWebViewCreated: (controller) {
        // นัดส่งสีที่ค้างอยู่
        _controller = controller;
        // เริ่มลองส่งสถานะทั้งหมดเมื่อ JavaScript พร้อม
        _scheduleColors();
        unawaited(_primePainterBridge(controller));
      // สคริปต์ที่ฉีดเข้าหน้า HTML ของ model-viewer
      },
      // ช่อง JavaScript ที่รับข้อความจากหน้าเว็บกลับมายัง Dart
      relatedJs: _painterScript!,
      // ช่อง ModelLoaded: JavaScript แจ้งว่าโมเดลโหลดเสร็จแล้ว
      javascriptChannels: {
        // ส่งสีทุกชิ้นส่วน
        JavascriptChannel('ModelLoaded', onMessageReceived: (_) async {
          // วาดสติกเกอร์และเส้นที่เก็บไว้
          await _applyAllColors();
          // ส่ง config เครื่องมือ
          await _renderStoredDecorations();
          // ส่งสติกเกอร์ที่เลือก
          await _syncEditorConfig();
          // ถ้ามีสติกเกอร์รอวางให้ส่งไปด้วย
          await selectSticker(widget.selectedStickerId);
          if (_pendingSticker != null) await _runEditor('setPendingSticker', _pendingSticker);
        }),
        // ช่อง DesignEvent: JavaScript ส่งเหตุการณ์การวาง ย้าย เลือก วาด
        JavascriptChannel('DesignEvent', onMessageReceived: (message) {
          // ส่งข้อความไปประมวลผลใน _handleDesignMessage
          _handleDesignMessage(message.message);
        }),
      },
    );
  }
}

// สตริงดิบ (raw string) เก็บโค้ด JavaScript ตัวเริ่มต้น (loader) ที่ฉีดเข้า WebView ต่อจากสคริปต์หลัก
// ไฟล์ tools/check_bootstrap.cjs ดึงข้อความตรงนี้ไปทดสอบ
const String _texturePainterLoaderJavaScript = r'''
  // JS: ฟังก์ชันเรียกตัวเองทันที
  (function() {
    // JS: สร้างคิวคำสั่งถ้ายังไม่มี (เก็บคำสั่งที่มาก่อนตัววาดพร้อม)
    if (!window.__shoePainterQueue) window.__shoePainterQueue = [];
    // JS: ถ้ายังไม่มีฟังก์ชัน callShoePainter
    if (!window.callShoePainter) {
      // JS: นิยามฟังก์ชัน callShoePainter รับชื่อเมธอดกับอาร์กิวเมนต์
      window.callShoePainter = function(method, args) {
        // JS: ดึงตัววาดปัจจุบัน
        const painter = window.shoePainter;
        // JS: ถ้าตัววาดพร้อมและมีเมธอดนั้น
        if (painter && typeof painter[method] === 'function') {
          // JS: เรียกเมธอดทันที
          return painter[method].apply(painter, args || []);
        }
        // JS: ถ้ายังไม่พร้อม เก็บคำสั่งเข้าคิวไว้
        window.__shoePainterQueue.push({ method: method, args: args || [] });
      };
    }
    // JS: ถ้าตัววาดเริ่มแล้วหรือกำลังโหลดอยู่ ไม่ต้องเริ่มซ้ำ
    if (window.shoePainter || window.__shoePainterLoading) return;
    // JS: ค้นหาองค์ประกอบ model-viewer ในหน้า
    const mv = document.querySelector('model-viewer');
    // JS: ถ้าไม่พบก็ออก
    if (!mv) return;
    // JS: ตั้งธงว่ากำลังโหลดตัววาด
    window.__shoePainterLoading = true;

    // JS: อ่านที่อยู่ของไฟล์โมเดลจาก model-viewer
    const modelUrl = mv.src || mv.getAttribute('src');
    // JS: ใช้ try ดัก error ตอนเริ่ม
    try {
      // JS: เริ่มตัววาด texture
      window.startShoePainter({
        // JS: ส่งที่อยู่โมเดล
        modelUrl: modelUrl,
        // JS: ส่งที่อยู่ตัวถอดรหัส Draco
        dracoDecoderUrl: window.__shoeDecoderUrl
      });
      // JS: ดึงคำสั่งทั้งหมดที่ค้างในคิวออกมา
      const queued = window.__shoePainterQueue.splice(0);
      // JS: วนทุกคำสั่งที่ค้าง
      queued.forEach(function(call) {
        // JS: เรียกใช้คำสั่งนั้นตอนนี้ตัววาดพร้อมแล้ว
        window.callShoePainter(call.method, call.args);
      });
    // JS: ถ้าเริ่มไม่สำเร็จ
    } catch (error) {
      // JS: ปลดธงกำลังโหลด
      window.__shoePainterLoading = false;
      // JS: สร้างกล่องข้อความแสดง error
      const message = document.createElement('div');
      // JS: ใส่ข้อความ error
      message.textContent = 'เปิดเครื่องมือตกแต่งไม่สำเร็จ: ' + error.message;
      // JS: ตั้งสไตล์ให้ปิดทั้งหน้าจอด้วยพื้นขาวตัวอักษรแดง
      message.style.cssText = 'position:fixed;inset:0;background:white;color:red;z-index:999;padding:16px';
      // JS: ใส่กล่องลงในหน้า
      document.body.appendChild(message);
      // JS: พิมพ์ error ลง console
      console.error(error);
    }
  })();
''';
