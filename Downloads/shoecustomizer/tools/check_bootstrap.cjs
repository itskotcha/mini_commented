// ไฟล์ทดสอบนี้รันด้วย node tools/check_bootstrap.cjs เพื่อตรวจโค้ดเริ่มต้น (loader) ที่ฝังอยู่ใน shoe_model_viewer.dart
// นำเข้าโมดูลอ่านไฟล์ของ Node
const fs = require('node:fs');
// นำเข้าโมดูล vm สำหรับรันโค้ดในกล่องทราย
const vm = require('node:vm');
// นำเข้า assert แบบเข้มงวด
const assert = require('node:assert/strict');
// อ่านไฟล์ Dart เป็นข้อความ
const dart = fs.readFileSync('lib/widgets/shoe_model_viewer.dart', 'utf8');
// ตัดเฉพาะข้อความ JavaScript ที่อยู่ในสตริงดิบ _texturePainterLoaderJavaScript ออกมา
const loader = dart.split("const String _texturePainterLoaderJavaScript = r'''")[1].split("''';")[0];
// ลิสต์เก็บบันทึกการเรียก setConfig จากตัววาดจำลอง
const calls = [];
// จำลองอ็อบเจ็กต์ window ของเบราว์เซอร์
const window = {
  // URL ของตัวถอดรหัส Draco จำลอง
  __shoeDecoderUrl: 'data:application/javascript;base64,AAAA',
  // คิวคำสั่งที่ค้างอยู่ก่อนตัววาดพร้อม มีคำสั่ง setConfig หนึ่งรายการ
  __shoePainterQueue: [{method: 'setConfig', args: [{tool: 'draw'}]}],
  // ฟังก์ชัน startShoePainter จำลอง
  startShoePainter(options) {
    // ตรวจว่า modelUrl เป็น /model
    assert.equal(options.modelUrl, '/model');
    // ตรวจว่า URL ตัวถอดรหัสถูกส่งต่อมาถูกต้อง
    assert.equal(options.dracoDecoderUrl, this.__shoeDecoderUrl);
    // สร้างตัววาดจำลองที่บันทึกการเรียก setConfig ลงลิสต์
    this.shoePainter = {setConfig(config) { calls.push(config); }};
  },
// จบ window จำลอง
};
// จำลองอ็อบเจ็กต์ document
const document = {
  // querySelector คืนองค์ประกอบ model-viewer จำลองที่มี src เป็น /model
  querySelector() { return {src: '/model'}; },
  // createElement ต้องไม่ถูกเรียก (ถ้าถูกเรียก แปลว่าเข้าสู่ทางแสดง error) จึงโยน error ให้ทดสอบล้มเหลว
  createElement() { throw new Error('Unexpected network script or error UI'); },
};
// รันโค้ด loader ในบริบทที่มี window, document และ console จำลอง
vm.runInNewContext(loader, {window, document, console});
// ตรวจว่าคำสั่งที่ค้างในคิวถูกเรียกแล้ว 1 ครั้ง
assert.equal(calls.length, 1);
// ตรวจว่าคำสั่งที่ค้างนั้นคือ tool เป็น draw
assert.equal(calls[0].tool, 'draw');
// ทดลองเรียกคำสั่งสดผ่าน callShoePainter
window.callShoePainter('setConfig', [{tool: 'sticker'}]);
// ตรวจว่าคำสั่งสดถูกส่งไปถึงตัววาดทันที
assert.equal(calls[1].tool, 'sticker');
// ตรวจว่าคิวว่างแล้ว
assert.equal(window.__shoePainterQueue.length, 0);
// พิมพ์ข้อความว่าผ่านการทดสอบ
console.log('PASS: /model bootstrap, embedded decoder, queued and live commands');
