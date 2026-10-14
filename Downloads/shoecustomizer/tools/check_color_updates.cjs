// ไฟล์ทดสอบนี้รันด้วย node tools/check_color_updates.cjs เพื่อตรวจว่าการเปลี่ยนสีถี่ ๆ ถูกรวมเป็นการวาด texture ครั้งเดียวต่อเฟรม
// นำเข้าโมดูลอ่านไฟล์ของ Node
const fs = require('node:fs');
// นำเข้าโมดูล vm สำหรับรันโค้ดในกล่องทราย (sandbox) แยกจากโปรแกรมหลัก
const vm = require('node:vm');
// นำเข้า assert แบบเข้มงวดสำหรับตรวจผลลัพธ์ ถ้าไม่ตรงจะ error
const assert = require('node:assert/strict');
// อ่านซอร์สโค้ดตัววาด tools/shoe_painter_entry.js เป็นข้อความ
const source = fs.readFileSync('tools/shoe_painter_entry.js', 'utf8');
// ตัดเฉพาะส่วนฟังก์ชัน rebuildMaterial ถึงก่อน rebuildAll ออกมาทดสอบ
const functions = source.slice(source.indexOf('  function rebuildMaterial('), source.indexOf('  function rebuildAll('));
// ตัดเฉพาะส่วนลูปแสดงผลรายเฟรมออกมาทดสอบ
const frame = source.slice(source.indexOf('  function animate()'), source.indexOf('  animate();'));
// ตัวนับจำนวนครั้งที่เรียกวาดภาพพื้นฐาน
let paints = 0;
// สถานะจำลองของ material หนึ่งตัว มี texture จำลอง
const state = {texture: {needsUpdate: false}};
// กล่องทราย: ตัวแปรและฟังก์ชันจำลองที่โค้ดที่ตัดมาต้องใช้
const sandbox = {
  // ชุด material ที่ต้องวาดใหม่ และแผนที่สถานะที่มี material ลำดับ 0
  dirtyMaterials: new Set(), materialStates: new Map([[0, state]]),
  // ตัวแก้ไขจำลองที่ไม่มีเส้นหรือสติกเกอร์
  editor: {strokes: [], stickers: []},
  // ฟังก์ชันจำลอง ที่นับจำนวนครั้งการวาดภาพพื้นฐาน และฟังก์ชันอื่นเป็นฟังก์ชันว่าง
  copyBaseImage() { paints++; }, drawStrokeOnState() {}, drawSticker() {},
  // ฟังก์ชันขอเฟรมถัดไปจำลอง (ไม่ทำอะไร จะได้ไม่วนลูปไม่รู้จบ)
  requestAnimationFrame() {}, controls: {update() {}},
  // ตัวควบคุมกล้อง ตัวเรนเดอร์ ฉาก และกล้อง จำลอง
  renderer: {render() {}}, scene: {}, camera: {},
};
// สร้างบริบท vm จากกล่องทราย
vm.createContext(sandbox);
// รันโค้ดที่ตัดมาในบริบทนั้น
vm.runInContext(functions + frame, sandbox);
// เรียก rebuildMaterial 100 ครั้งติดกัน (จำลองการเปลี่ยนสีรัว ๆ)
for (let i = 0; i < 100; i++) sandbox.rebuildMaterial(0);
// ตรวจว่ายังไม่มีการวาดจริงเลย เพราะแค่จดไว้ว่าต้องวาดใหม่
assert.equal(paints, 0);
// จำลองหนึ่งเฟรม
sandbox.animate();
// ตรวจว่าวาดเพียง 1 ครั้ง แม้เรียก 100 ครั้ง
assert.equal(paints, 1);
// ตรวจว่า texture ถูกตั้งธงให้อัปเดต
assert.equal(state.texture.needsUpdate, true);
// จำลองอีกหนึ่งเฟรมที่ไม่มีอะไรเปลี่ยน
sandbox.animate();
// ตรวจว่าไม่วาดซ้ำ
assert.equal(paints, 1);
// สั่งวาดใหม่อีกครั้ง
sandbox.rebuildMaterial(0);
// จำลองเฟรมถัดไป
sandbox.animate();
// ตรวจว่าวาดเพิ่มเป็น 2 ครั้ง
assert.equal(paints, 2);
// พิมพ์ข้อความว่าผ่านการทดสอบ
console.log('PASS: 100 repeated updates produce one texture repaint; idle frames do not repaint');
