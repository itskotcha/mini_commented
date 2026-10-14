// นำเข้า three.js ทั้งหมดเป็นชื่อ THREE (ไลบรารี 3D บน WebGL)
import * as THREE from 'three';
// นำเข้า GLTFLoader สำหรับโหลดไฟล์โมเดล GLB/glTF
import {GLTFLoader} from 'three/examples/jsm/loaders/GLTFLoader.js';
// นำเข้า DRACOLoader สำหรับถอดรหัสโมเดลที่บีบอัดด้วย Draco
import {DRACOLoader} from 'three/examples/jsm/loaders/DRACOLoader.js';
// นำเข้า OrbitControls ให้ผู้ใช้หมุน/ซูมกล้องรอบโมเดลด้วยนิ้วหรือเมาส์
import {OrbitControls} from 'three/examples/jsm/controls/OrbitControls.js';

// ขนาด texture ที่ใช้วาด (1024 คูณ 1024 พิกเซล) ต่อหนึ่ง material
const TEXTURE_SIZE = 1024;

// ส่งเหตุการณ์กลับไปยัง Flutter ผ่านช่อง DesignEvent (ชนิดเหตุการณ์ id และตำแหน่งผิว)
function post(type, id, surface) {
  // ถ้าไม่มีช่อง DesignEvent (เช่น รันนอกแอป) ก็ไม่ทำอะไร
  if (!window.DesignEvent) return;
  // แปลงข้อมูลเป็นข้อความ JSON แล้วส่งให้ Flutter
  window.DesignEvent.postMessage(JSON.stringify({
    // ชนิดเหตุการณ์ เช่น stickerPlaced
    type,
    // id (ถ้าไม่มีใส่ null)
    id: id || null,
    // ตำแหน่งผิว (ถ้าไม่มีใส่ null)
    surface: surface || null,
  }));
}

// เข้ารหัสตำแหน่งบนผิวเป็นข้อความ เลข material และพิกัด UV ที่ทศนิยม 7 ตำแหน่ง
function encodeTarget(materialIndex, u, v) {
  // รูปแบบ uv แล้วตามด้วยเลข material, u, v คั่นด้วยเครื่องหมายขีดตั้ง
  return `uv|${materialIndex}|${u.toFixed(7)}|${v.toFixed(7)}`;
}

// ถอดรหัสข้อความตำแหน่งกลับเป็นอ็อบเจ็กต์ (คืน null ถ้ารูปแบบไม่ถูกต้อง)
function decodeTarget(value) {
  // ถ้าไม่ใช่ข้อความ คืน null
  if (typeof value !== 'string') return null;
  // แยกข้อความด้วยเครื่องหมายขีดตั้ง
  const parts = value.split('|');
  // ต้องมี 4 ส่วนและส่วนแรกเป็นคำว่า uv
  if (parts.length !== 4 || parts[0] !== 'uv') return null;
  // เลข material
  const materialIndex = Number(parts[1]);
  // พิกัด u
  const u = Number(parts[2]);
  // พิกัด v
  const v = Number(parts[3]);
  // ถ้าค่าใดไม่ใช่ตัวเลขที่ใช้ได้ คืน null
  if (![materialIndex, u, v].every(Number.isFinite)) return null;
  // คืนอ็อบเจ็กต์ materialIndex, u, v
  return {materialIndex, u, v};
}

// แปลงสีให้เป็นรหัสฐานสิบหกมาตรฐาน (ถ้าไม่มีใช้สีดำ)
function parseHexColor(value) {
  // ให้ three.js แปลงข้อความสีเป็นอ็อบเจ็กต์สี
  const color = new THREE.Color(value || '#000000');
  // คืนรหัสสี เช่น #aabbcc
  return `#${color.getHexString()}`;
}

// โหลดรูปสติกเกอร์ และทำให้พื้นหลังสีขาวโปร่งใส คืน Promise ของ canvas
function loadStickerImage(src) {
  // สร้าง Promise ที่สำเร็จเมื่อรูปโหลดเสร็จ
  return new Promise((resolve, reject) => {
    // สร้างอ็อบเจ็กต์รูป
    const image = new Image();
    // เมื่อรูปโหลดเสร็จ
    image.onload = () => {
      // ใช้ try เพื่อส่ง error ต่อให้ reject
      try {
        // สร้าง canvas ชั่วคราว
        const canvas = document.createElement('canvas');
        // ด้านที่ยาวที่สุดไม่เกิน 768 พิกเซล
        const maxSide = 768;
        // อัตราย่อส่วน (ไม่ขยายใหญ่กว่าเดิม)
        const ratio = Math.min(1, maxSide / Math.max(image.naturalWidth || image.width, image.naturalHeight || image.height));
        // ตั้งความกว้างของ canvas ตามอัตราย่อ
        canvas.width = Math.max(1, Math.round((image.naturalWidth || image.width) * ratio));
        // ตั้งความสูงของ canvas ตามอัตราย่อ
        canvas.height = Math.max(1, Math.round((image.naturalHeight || image.height) * ratio));
        // ขอ context 2D (ตั้งค่าว่าจะอ่านพิกเซลบ่อย)
        const ctx = canvas.getContext('2d', {willReadFrequently: true});
        // วาดรูปลงบน canvas
        ctx.drawImage(image, 0, 0, canvas.width, canvas.height);

        // ลบเฉพาะพื้นขาวที่เชื่อมกับขอบรูป เพื่อไม่ทำลายรายละเอียดสีขาวด้านในสติกเกอร์
        // ลบเฉพาะพื้นขาวที่เชื่อมกับขอบรูป เพื่อไม่ทำลายรายละเอียดสีขาวด้านในสติกเกอร์
        // อ่านพิกเซลทั้งภาพ
        const pixels = ctx.getImageData(0, 0, canvas.width, canvas.height);
        // อาร์เรย์ข้อมูลพิกเซล (RGBA ต่อพิกเซล)
        const data = pixels.data;
        // ความกว้างภาพ
        const width = canvas.width;
        // ความสูงภาพ
        const height = canvas.height;
        // อาร์เรย์บันทึกว่าพิกเซลไหนเคยเข้าคิวแล้ว
        const visited = new Uint8Array(width * height);
        // คิวของพิกเซลที่จะประมวลผล (วิธีค้นหาแบบกว้างหรือ BFS)
        const queue = new Int32Array(width * height);
        // ตัวชี้ตำแหน่งอ่านคิว
        let read = 0;
        // ตัวชี้ตำแหน่งเขียนคิว
        let write = 0;
        // ฟังก์ชันตรวจว่าพิกเซลเป็นพื้นหลังหรือไม่
        const isBackground = (pixel) => {
          // ตำแหน่งของพิกเซลในอาร์เรย์ (คูณ 4 เพราะมี RGBA)
          const offset = pixel * 4;
          // ถ้าโปร่งใสอยู่แล้ว (อัลฟาไม่เกิน 8) นับเป็นพื้นหลัง
          if (data[offset + 3] <= 8) return true;
          // ค่าสีต่ำสุดของ R, G, B
          const min = Math.min(data[offset], data[offset + 1], data[offset + 2]);
          // ค่าสีสูงสุดของ R, G, B
          const max = Math.max(data[offset], data[offset + 1], data[offset + 2]);
          // เป็นพื้นหลังถ้าสว่างมาก (ต่ำสุดตั้งแต่ 225) และเกือบเป็นสีเทา (ต่างกันไม่เกิน 18)
          return min >= 225 && max - min <= 18;
        };
        // ฟังก์ชันใส่พิกเซลเข้าคิว
        const enqueue = (pixel) => {
          // ถ้าเคยเข้าคิวแล้วหรือไม่ใช่พื้นหลัง ข้ามไป
          if (visited[pixel] || !isBackground(pixel)) return;
          // ทำเครื่องหมายว่าเข้าคิวแล้ว
          visited[pixel] = 1;
          // ใส่ลงคิว
          queue[write++] = pixel;
        };
        // ใส่พิกเซลขอบบนและขอบล่างทุกคอลัมน์เป็นจุดเริ่มต้น
        for (let x = 0; x < width; x++) {
          // ขอบบน
          enqueue(x);
          // ขอบล่าง
          enqueue((height - 1) * width + x);
        }
        // ใส่พิกเซลขอบซ้ายและขอบขวาทุกแถวเป็นจุดเริ่มต้น
        for (let y = 0; y < height; y++) {
          // ขอบซ้าย
          enqueue(y * width);
          // ขอบขวา
          enqueue(y * width + width - 1);
        }
        // วนจนกว่าคิวจะหมด (ลามจากขอบเข้าไปด้านในเฉพาะส่วนที่เป็นพื้นหลังต่อเนื่อง)
        while (read < write) {
          // ดึงพิกเซลจากคิว
          const pixel = queue[read++];
          // คำนวณพิกัด x
          const x = pixel % width;
          // คำนวณพิกัด y
          const y = Math.floor(pixel / width);
          // ตำแหน่งในอาร์เรย์
          const offset = pixel * 4;
          // ค่าสีต่ำสุดของพิกเซลนี้
          const min = Math.min(data[offset], data[offset + 1], data[offset + 2]);
          // ตั้งค่าอัลฟา ถ้าขาวมากให้โปร่งใสเต็มที่ ไม่เช่นนั้นไล่ความโปร่งใสตามความขาว
          data[offset + 3] = min >= 246 ? 0 : Math.round(data[offset + 3] * (246 - min) / 21);
          // ใส่เพื่อนบ้านด้านซ้ายเข้าคิว
          if (x > 0) enqueue(pixel - 1);
          // ใส่เพื่อนบ้านด้านขวา
          if (x + 1 < width) enqueue(pixel + 1);
          // ใส่เพื่อนบ้านด้านบน
          if (y > 0) enqueue(pixel - width);
          // ใส่เพื่อนบ้านด้านล่าง
          if (y + 1 < height) enqueue(pixel + width);
        }
        // ทำขอบโปร่งใสที่ติดกับพื้นหลังให้นุ่มขึ้นหนึ่งพิกเซล
        // ทำขอบโปร่งใสที่ติดกับพื้นหลังให้นุ่มขึ้นหนึ่งพิกเซล
        // วนทุกพิกเซล
        for (let pixel = 0; pixel < visited.length; pixel++) {
          // ข้ามพิกเซลที่ไม่ใช่พื้นหลัง
          if (!visited[pixel]) continue;
          // คำนวณพิกัด x
          const x = pixel % width;
          // คำนวณพิกัด y
          const y = Math.floor(pixel / width);
          // เตรียมลิสต์เพื่อนบ้าน
          const neighbors = [];
          // ซ้าย
          if (x > 0) neighbors.push(pixel - 1);
          // ขวา
          if (x + 1 < width) neighbors.push(pixel + 1);
          // บน
          if (y > 0) neighbors.push(pixel - width);
          // ล่าง
          if (y + 1 < height) neighbors.push(pixel + width);
          // วนเพื่อนบ้านแต่ละตัว
          for (const neighbor of neighbors) {
            // ถ้าเพื่อนบ้านไม่ใช่พื้นหลัง (คือขอบของตัวสติกเกอร์)
            if (!visited[neighbor]) {
              // ตำแหน่งช่องอัลฟาของเพื่อนบ้าน
              const alphaOffset = neighbor * 4 + 3;
              // ลดความทึบลงเหลือ 82 เปอร์เซ็นต์ ให้ขอบนุ่มขึ้น
              data[alphaOffset] = Math.round(data[alphaOffset] * 0.82);
            }
          }
        }
        // เขียนพิกเซลที่แก้แล้วกลับลง canvas
        ctx.putImageData(pixels, 0, 0);
        // ส่ง canvas ที่ได้ออกไปเป็นผลลัพธ์ของ Promise
        resolve(canvas);
      // ถ้าเกิดข้อผิดพลาด
      } catch (error) {
        // ปฏิเสธ Promise
        reject(error);
      }
    // จบ handler onload
    };
    // ถ้ารูปโหลดไม่ได้ให้ปฏิเสธ Promise
    image.onerror = reject;
    // เริ่มโหลดรูปจากแหล่ง src
    image.src = src;
  });
}

// ฟังก์ชันหลัก เริ่มระบบ 3D ทั้งหมด (Flutter เรียกผ่าน window.startShoePainter)
function startShoePainter(options) {
  // ถ้าเริ่มแล้วไม่ต้องทำซ้ำ
  if (window.shoePainter) return;

  // หาองค์ประกอบ model-viewer ที่แพ็กเกจ model_viewer_plus สร้างไว้ในหน้า
  const modelViewer = document.querySelector('model-viewer');
  // สร้างกล่อง div สำหรับวาง 3D ของเราเอง
  const mount = document.createElement('div');
  // ตั้ง id
  mount.id = 'shoe-three-mount';
  // ตั้งสไตล์ให้ปิดทั้งหน้าจอ พื้นขาว และปิดการเลื่อนหน้าด้วยการสัมผัส
  mount.style.cssText = 'position:fixed;inset:0;overflow:hidden;background:#fff;touch-action:none;';
  // ใส่กล่องลงในหน้าเว็บ
  document.body.appendChild(mount);

  // สร้างข้อความกำลังโหลด
  const loading = document.createElement('div');
  // ข้อความ กำลังโหลดรองเท้า 3D...
  loading.textContent = 'กำลังโหลดรองเท้า 3D...';
  // จัดข้อความให้อยู่กลางกล่อง สีเทา
  loading.style.cssText = 'position:absolute;inset:0;display:flex;align-items:center;justify-content:center;color:#777;font:14px sans-serif;';
  // ใส่ข้อความลงกล่อง
  mount.appendChild(loading);
  // ซ่อน model-viewer เดิม (เราจะแสดงโมเดลเองด้วย three.js แทน)
  if (modelViewer) modelViewer.style.visibility = 'hidden';

  // สร้างตัวเรนเดอร์ WebGL เปิดลบรอยหยัก พื้นหลังโปร่งใสได้ และเก็บบัฟเฟอร์ภาพไว้
  const renderer = new THREE.WebGLRenderer({antialias: true, alpha: true, preserveDrawingBuffer: true});
  // ตั้งความละเอียดพิกเซลตามหน้าจอ แต่ไม่เกิน 2 เท่า เพื่อประหยัดแรงเครื่อง
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  // ตั้งขนาดตามหน้าต่าง
  renderer.setSize(window.innerWidth, window.innerHeight);
  // ใช้การเข้ารหัสสี sRGB
  renderer.outputEncoding = THREE.sRGBEncoding;
  // ใช้ tone mapping แบบ ACES Filmic ให้สีดูเป็นธรรมชาติ
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  // ความสว่างของ tone mapping เป็น 0.9
  renderer.toneMappingExposure = 0.9;
  // ตั้งสไตล์ canvas ให้เต็มกล่อง
  renderer.domElement.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;display:block;';
  // ใส่ canvas ลงกล่อง
  mount.appendChild(renderer.domElement);

  // สร้างฉาก 3D
  const scene = new THREE.Scene();
  // พื้นหลังฉากสีเทาอ่อน
  scene.background = new THREE.Color(0xf1f2f5);
  // สร้างกล้องแบบเปอร์สเปกทีฟ มุมมอง 38 องศา
  const camera = new THREE.PerspectiveCamera(38, window.innerWidth / window.innerHeight, 0.01, 1000);
  // สร้างตัวควบคุมกล้องแบบโคจร
  const controls = new OrbitControls(camera, renderer.domElement);
  // เปิดแรงเฉื่อยให้หมุนนุ่มนวล
  controls.enableDamping = true;
  // ค่าแรงหน่วง 0.08
  controls.dampingFactor = 0.08;
  // ปิดการเลื่อนแพนกล้อง
  controls.enablePan = false;
  // ระยะซูมใกล้สุดเริ่มต้น (จะถูกตั้งใหม่หลังโหลดโมเดล)
  controls.minDistance = 0.1;

  // ไฟรอบทิศ (ฟ้าอ่อน/พื้นมืด) ความเข้ม 0.65
  scene.add(new THREE.HemisphereLight(0xffffff, 0x777788, 0.65));
  // ไฟหลักแบบทิศทาง ความเข้ม 0.85
  const keyLight = new THREE.DirectionalLight(0xffffff, 0.85);
  // ตำแหน่งไฟหลัก
  keyLight.position.set(4, 7, 6);
  // ใส่ไฟหลักลงฉาก
  scene.add(keyLight);
  // ไฟเสริม ความเข้ม 0.3
  const fillLight = new THREE.DirectionalLight(0xffffff, 0.3);
  // ตำแหน่งไฟเสริม (ด้านตรงข้าม)
  fillLight.position.set(-5, 2, -4);
  // ใส่ไฟเสริมลงฉาก
  scene.add(fillLight);

  // Raycaster ยิงรังสีจากจุดที่แตะเพื่อหาว่าชนผิวรองเท้าตรงไหน
  const raycaster = new THREE.Raycaster();
  // ตำแหน่งนิ้วแบบพิกัดมาตรฐาน -1 ถึง 1
  const pointer = new THREE.Vector2();
  // แผนที่เก็บสถานะการวาดของแต่ละ material
  const materialStates = new Map();
  // แผนที่เก็บสีที่ตั้งให้แต่ละ material
  const materialColors = new Map();
  // ชุดของ material ที่ต้องวาด texture ใหม่ในเฟรมถัดไป
  const dirtyMaterials = new Set();
  // ออบเจ็กต์สถานะของตัวแก้ไข
  const editor = {
    // เครื่องมือปัจจุบัน
    tool: 'color',
    // สีปากกา
    color: '#000000',
    // ขนาดปากกา
    size: 7,
    // ชนิดหัวปากกา
    brush: 'round',
    // เปิดให้แก้ไขหรือไม่
    editable: false,
    // สติกเกอร์ที่รอวาง
    pendingSticker: null,
    // id สติกเกอร์ที่เลือก
    selectedStickerId: null,
    // id สติกเกอร์ที่กำลังลากย้าย
    draggingStickerId: null,
    // กำลังวาดเส้นอยู่หรือไม่
    drawing: false,
    // id เส้นที่กำลังวาด
    strokeId: null,
    // ตำแหน่ง X ของนิ้วครั้งล่าสุด (ใช้ลดจุดถี่)
    lastClientX: -9999,
    // ตำแหน่ง Y ของนิ้วครั้งล่าสุด
    lastClientY: -9999,
    // รายการสติกเกอร์ทั้งหมด
    stickers: [],
    // รายการเส้นทั้งหมด
    strokes: [],
    // โมเดลโหลดเสร็จแล้วหรือยัง
    loaded: false,
  };

  // หา material และพิกัด UV จากจุดที่รังสีชน
  function resolveMaterial(hit) {
    // ดึงวัตถุที่ถูกชน
    const object = hit.object;
    // ดึง material ของวัตถุ
    let material = object.material;
    // ถ้า material เป็นอาร์เรย์ (หลายกลุ่มบนวัตถุเดียว)
    if (Array.isArray(material)) {
      // เลือกตัวที่ตรงกับกลุ่มของหน้าที่ชน
      material = material[hit.face?.materialIndex || 0];
    }
    // อ่านเลข material เดิมในไฟล์ GLB ที่เก็บไว้ใน userData
    const materialIndex = material?.userData?.gltfMaterialIndex;
    // ถ้าไม่มีเลข หรือจุดที่ชนไม่มีพิกัด UV ให้คืน null
    if (!Number.isInteger(materialIndex) || !hit.uv) return null;
    // คืนข้อมูลที่ได้
    return {
      // เลข material
      materialIndex,
      // พิกัด u (ทำให้อยู่ในช่วง 0 ถึง 1)
      u: THREE.MathUtils.euclideanModulo(hit.uv.x, 1),
      // พิกัด v (ทำให้อยู่ในช่วง 0 ถึง 1)
      v: THREE.MathUtils.euclideanModulo(hit.uv.y, 1),
    };
  }

  // หาตำแหน่งผิวรองเท้าจากจุดสัมผัส
  function hitAt(event) {
    // ถ้าโมเดลยังโหลดไม่เสร็จ คืน null
    if (!editor.loaded) return null;
    // ขนาดและตำแหน่งของ canvas บนหน้าจอ
    const rect = renderer.domElement.getBoundingClientRect();
    // แปลงพิกัด X ของนิ้วเป็นพิกัด -1 ถึง 1
    pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    // แปลงพิกัด Y (กลับแกน)
    pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    // ตั้งรังสีจากกล้องผ่านจุดที่แตะ
    raycaster.setFromCamera(pointer, camera);
    // หาวัตถุทุกชิ้นที่รังสีชน เรียงจากใกล้ไปไกล
    const hits = raycaster.intersectObjects(scene.children, true);
    // วนทุกจุดที่ชน
    for (const hit of hits) {
      // ข้ามถ้าไม่ใช่ mesh หรือถูกกำหนดให้ไม่รับการวาด
      if (!hit.object.isMesh || hit.object.userData.ignorePaint) continue;
      // หา material และ UV ของจุดนี้
      const target = resolveMaterial(hit);
      // ถ้าได้ ใช้จุดแรกที่ใช้ได้ทันที
      if (target) return target;
    }
    // ไม่พบจุดที่ใช้ได้ คืน null
    return null;
  }

  // วาดภาพพื้นฐานของ material ลง canvas (สีที่เลือกคูณกับลายเดิม)
  function copyBaseImage(state) {
    // ดึง context 2D
    const ctx = state.ctx;
    // ขนาดของ canvas
    const size = state.canvas.width;
    // บันทึกสถานะการวาด
    ctx.save();
    // โหมดผสมปกติ
    ctx.globalCompositeOperation = 'source-over';
    // ความทึบเต็ม
    ctx.globalAlpha = 1;
    // ล้าง canvas
    ctx.clearRect(0, 0, size, size);
    // ถ้ามีรูป texture เดิมของโมเดล
    if (state.originalCanvas) {
      // วาดรูปเดิมลงไปก่อน
      ctx.drawImage(state.originalCanvas, 0, 0, size, size);
      // เปลี่ยนเป็นโหมดคูณสี (multiply)
      ctx.globalCompositeOperation = 'multiply';
      // ตั้งสีที่เลือก
      ctx.fillStyle = state.color;
      // ระบายสีทับทั้งภาพ ทำให้ลายเดิมถูกย้อมด้วยสีใหม่
      ctx.fillRect(0, 0, size, size);
      // เปลี่ยนเป็นโหมดคงไว้เฉพาะส่วนที่ทับกับรูปเดิม
      ctx.globalCompositeOperation = 'destination-in';
      // วาดรูปเดิมอีกครั้งเพื่อตัดส่วนนอกลายออก (คงรูปทรงโปร่งใสเดิมไว้)
      ctx.drawImage(state.originalCanvas, 0, 0, size, size);
    // ถ้าไม่มีรูปเดิม
    } else {
      // ตั้งสีที่เลือก
      ctx.fillStyle = state.color;
      // ระบายสีทั้ง canvas
      ctx.fillRect(0, 0, size, size);
    }
    // คืนสถานะการวาด
    ctx.restore();
  }

  // วาดสติกเกอร์หนึ่งชิ้นลง canvas ของ material (selected คือชิ้นที่ถูกเลือก)
  function drawSticker(state, sticker, selected = false) {
    // ถ้ารูปยังโหลดไม่เสร็จ ข้าม
    if (!sticker.image) return;
    // ถอดรหัสตำแหน่งของสติกเกอร์
    const target = decodeTarget(sticker.surface);
    // ถ้าตำแหน่งไม่ถูกต้อง หรือสติกเกอร์ไม่ได้อยู่บน material นี้ ข้าม
    if (!target || target.materialIndex !== state.index) return;
    // ขนาดของ canvas
    const size = state.canvas.width;
    // ความกว้างสติกเกอร์ 150 พิกเซล คูณอัตราขยาย
    const width = 150 * Number(sticker.scale || 1);
    // อัตราส่วนกว้างต่อสูงของรูป
    const aspect = sticker.image.width / Math.max(1, sticker.image.height);
    // ความสูงคำนวณจากความกว้างและอัตราส่วน
    const height = width / Math.max(0.2, aspect);
    // ตำแหน่ง X บน canvas จากค่า u
    const x = target.u * size;
    // ตำแหน่ง Y บน canvas จากค่า v
    const y = target.v * size;
    // ดึง context 2D
    const ctx = state.ctx;
    // บันทึกสถานะ
    ctx.save();
    // ย้ายจุดกำเนิดไปที่ตำแหน่งสติกเกอร์
    ctx.translate(x, y);
    // หมุนตามมุมของสติกเกอร์
    ctx.rotate(Number(sticker.rotation || 0));
    // ถ้าเป็นชิ้นที่เลือกอยู่และอยู่ในโหมดสติกเกอร์
    if (selected && editor.editable && editor.tool === 'sticker') {
      // สีกรอบม่วง
      ctx.strokeStyle = '#7c3aed';
      // ความหนากรอบ 7
      ctx.lineWidth = 7;
      // ลายเส้นประ
      ctx.setLineDash([14, 8]);
      // วาดกรอบสี่เหลี่ยมล้อมสติกเกอร์
      ctx.strokeRect(-width / 2 - 7, -height / 2 - 7, width + 14, height + 14);
    }
    // วาดรูปสติกเกอร์โดยให้จุดกึ่งกลางอยู่ที่ตำแหน่ง
    ctx.drawImage(sticker.image, -width / 2, -height / 2, width, height);
    // คืนสถานะ
    ctx.restore();
  }

  // ตั้งค่าสไตล์ปากกาตามชนิดหัวปากกา
  function setBrushStyle(ctx, stroke, size) {
    // ความหนาพื้นฐาน ปรับตามขนาด texture
    const base = Math.max(2, Number(stroke.size || 7)) * (size / 420);
    // สีเส้น
    ctx.strokeStyle = parseHexColor(stroke.color);
    // สีเติม
    ctx.fillStyle = parseHexColor(stroke.color);
    // ปลายเส้น ถ้าเป็นมาร์กเกอร์ใช้ปลายเหลี่ยม ไม่เช่นนั้นปลายมน
    ctx.lineCap = stroke.brush === 'marker' ? 'square' : 'round';
    // ข้อต่อเส้นเป็นมน
    ctx.lineJoin = 'round';
    // ความหนาเส้น ไฮไลต์หนาขึ้น 2.25 เท่า
    ctx.lineWidth = stroke.brush === 'highlighter' ? base * 2.25 : base;
    // ความทึบ ไฮไลต์โปร่งใสมาก (0.34)
    ctx.globalAlpha = stroke.brush === 'highlighter' ? 0.34 : 1;
  }

  // วาดเส้นหนึ่งเส้นลง canvas ของ material
  function drawStrokeOnState(state, stroke) {
    // ถอดรหัสตำแหน่งทุกจุดของเส้น ตัดจุดที่ไม่ถูกต้องออก
    const points = (stroke.surfaces || []).map(decodeTarget).filter(Boolean);
    // ถ้าไม่มีจุดเลย ออก
    if (!points.length) return;
    // ขนาดของ canvas
    const size = state.canvas.width;
    // ดึง context 2D
    const ctx = state.ctx;
    // บันทึกสถานะ
    ctx.save();
    // ตั้งสไตล์ปากกา
    setBrushStyle(ctx, stroke, size);
    // จุดก่อนหน้า (null คือยังไม่มี)
    let previous = null;
    // วนทุกจุดของเส้น
    for (let i = 0; i < points.length; i++) {
      // จุดปัจจุบัน
      const point = points[i];
      // ถ้าจุดนี้อยู่คนละ material กับที่กำลังวาด
      if (point.materialIndex !== state.index) {
        // ตัดการเชื่อมกับจุดก่อนหน้า
        previous = null;
        // ข้ามไปจุดถัดไป
        continue;
      }
      // ตำแหน่ง X บน canvas
      const x = point.u * size;
      // ตำแหน่ง Y บน canvas
      const y = point.v * size;
      // ถ้าเป็นหัวสเปรย์
      if (stroke.brush === 'spray') {
        // รัศมีของละอองสเปรย์
        const radius = Math.max(4, Number(stroke.size || 7) * size / 380);
        // ค่าตั้งต้นของตัวสุ่ม (กำหนดตายตัวจากลำดับจุด เพื่อให้วาดซ้ำแล้วได้ลายเดิมเสมอ)
        let seed = (i + 1) * 9301;
        // พ่นละออง 10 จุดต่อหนึ่งตำแหน่ง
        for (let dot = 0; dot < 10; dot++) {
          // สุ่มค่าถัดไปด้วยสูตรเชิงเส้นแบบกำหนดตายตัว
          seed = (seed * 49297 + 233280) % 999983;
          // สุ่มมุม
          const angle = (seed % 6283) / 1000;
          // สุ่มระยะจากจุดศูนย์กลาง
          const distance = ((seed % 1000) / 1000) * radius;
          // ความทึบของละออง 0.55
          ctx.globalAlpha = 0.55;
          // เริ่มเส้นทางใหม่
          ctx.beginPath();
          // วาดจุดละอองเป็นวงกลมเล็ก
          ctx.arc(x + Math.cos(angle) * distance, y + Math.sin(angle) * distance, Math.max(1, radius * 0.1), 0, Math.PI * 2);
          // ระบายสี
          ctx.fill();
        }
      // ถ้าไม่ใช่สเปรย์ และมีจุดก่อนหน้าบน material เดียวกัน
      } else if (previous && previous.materialIndex === point.materialIndex) {
        // ผลต่างของ u
        const dx = point.u - previous.u;
        // ผลต่างของ v
        const dy = point.v - previous.v;
        // ไม่ลากเส้นข้ามรอยต่อของ UV island
        // ไม่ลากเส้นข้ามรอยต่อของ UV island
        // ถ้าสองจุดอยู่ใกล้กันพอ
        if (dx * dx + dy * dy < 0.025) {
          // เริ่มเส้นทางใหม่
          ctx.beginPath();
          // ย้ายไปจุดก่อนหน้า
          ctx.moveTo(previous.u * size, previous.v * size);
          // ลากเส้นมายังจุดปัจจุบัน
          ctx.lineTo(x, y);
          // วาดเส้น
          ctx.stroke();
        // ถ้าห่างกันเกินไป (ข้ามรอยต่อของพื้นผิว)
        } else {
          // เริ่มเส้นทางใหม่
          ctx.beginPath();
          // วาดจุดกลมแทนการลากเส้น
          ctx.arc(x, y, ctx.lineWidth / 2, 0, Math.PI * 2);
          // ระบายสี
          ctx.fill();
        }
      // กรณีอื่น (จุดแรกของเส้น)
      } else {
        // เริ่มเส้นทางใหม่
        ctx.beginPath();
        // วาดจุดกลม
        ctx.arc(x, y, ctx.lineWidth / 2, 0, Math.PI * 2);
        // ระบายสี
        ctx.fill();
      }
      // จำจุดนี้เป็นจุดก่อนหน้า
      previous = point;
    }
    // คืนสถานะ
    ctx.restore();
  }

  // สั่งให้วาด material นี้ใหม่ (แค่จดไว้ ยังไม่วาดทันที เพื่อรวมการเปลี่ยนถี่ ๆ ให้เหลือครั้งเดียวต่อเฟรม)
  function rebuildMaterial(materialIndex) {
    // เพิ่มลงชุด material ที่ต้องวาดใหม่
    dirtyMaterials.add(Number(materialIndex));
  }

  // วาด texture ของ material ใหม่จริง ๆ
  function paintMaterial(materialIndex) {
    // ดึงสถานะของ material
    const state = materialStates.get(Number(materialIndex));
    // ถ้าไม่มีให้ออก
    if (!state) return;
    // วาดภาพพื้นฐาน (สีและลายเดิม)
    copyBaseImage(state);
    // วาดเส้นทุกเส้นทับ
    for (const stroke of editor.strokes) drawStrokeOnState(state, stroke);
    // วนทุกสติกเกอร์
    for (const sticker of editor.stickers) {
      // วาดสติกเกอร์ (ใส่กรอบถ้าเป็นชิ้นที่เลือก)
      drawSticker(state, sticker, sticker.id === editor.selectedStickerId);
    }
    // บอก three.js ว่า texture เปลี่ยนแล้ว ให้ส่งขึ้น GPU ใหม่
    state.texture.needsUpdate = true;
  }

  // สั่งวาดใหม่ทุก material
  function rebuildAll() {
    // วนทุก material แล้วจดว่าต้องวาดใหม่
    for (const index of materialStates.keys()) rebuildMaterial(index);
  }

  // ให้แน่ใจว่าสติกเกอร์มีรูปที่โหลดแล้ว
  function ensureStickerImage(sticker) {
    // ถ้าไม่มี src หรือโหลดแล้ว คืนสติกเกอร์เดิมทันที
    if (!sticker?.src || sticker.image) return Promise.resolve(sticker);
    // โหลดรูปและทำพื้นหลังโปร่งใส
    return loadStickerImage(sticker.src).then((image) => {
      // เก็บรูปลงสติกเกอร์
      sticker.image = image;
      // คืนสติกเกอร์
      return sticker;
    });
  }

  // หาสติกเกอร์ที่อยู่ใกล้จุดที่แตะ
  function stickerNear(target) {
    // วนจากชิ้นบนสุด (ท้ายลิสต์) ไปชิ้นล่างสุด
    for (let i = editor.stickers.length - 1; i >= 0; i--) {
      // สติกเกอร์ชิ้นที่ i
      const sticker = editor.stickers[i];
      // ถอดรหัสตำแหน่ง
      const point = decodeTarget(sticker.surface);
      // ข้ามถ้าไม่มีตำแหน่งหรือคนละ material
      if (!point || point.materialIndex !== target.materialIndex) continue;
      // รัศมีที่ถือว่าแตะโดน 0.085 คูณอัตราขยาย
      const radius = 0.085 * Number(sticker.scale || 1);
      // ผลต่าง u
      const du = target.u - point.u;
      // ผลต่าง v
      const dv = target.v - point.v;
      // ถ้าอยู่ในรัศมี คืนสติกเกอร์นี้
      if (du * du + dv * dv <= radius * radius) return sticker;
    }
    // ไม่พบ คืน null
    return null;
  }

  // เลือกสติกเกอร์ (notify คือแจ้ง Flutter ด้วยหรือไม่)
  function selectSticker(id, notify = false) {
    // จำค่าเดิม
    const before = editor.selectedStickerId;
    // ตั้งค่าใหม่ (null หมายถึงไม่เลือก)
    editor.selectedStickerId = id == null ? null : String(id);
    // ถ้าเปลี่ยนไป วาดใหม่เพื่อแสดงหรือซ่อนกรอบ
    if (before !== editor.selectedStickerId) rebuildAll();
    // ถ้าต้องแจ้ง ส่งเหตุการณ์ stickerSelected
    if (notify) post('stickerSelected', editor.selectedStickerId, null);
  }

  // เพิ่มหรืออัปเดตสติกเกอร์ (shouldPost คือแจ้ง Flutter ว่าวางแล้ว)
  function addSticker(data, shouldPost = false) {
    // ถ้าไม่มี id หรือตำแหน่งไม่ถูกต้อง ข้าม
    if (!data?.id || !decodeTarget(data.surface)) return;
    // หาสติกเกอร์เดิมที่ id ตรงกัน
    const existing = editor.stickers.find((item) => item.id === String(data.id));
    // ถ้ามีของเดิมใช้ของเดิม ไม่เช่นนั้นสร้างใหม่
    const sticker = existing || {
      // id
      id: String(data.id),
      // ข้อมูลรูป
      src: data.src,
      // ตำแหน่งบนผิว
      surface: data.surface,
      // ขนาด
      scale: Number(data.scale || 1),
      // มุมหมุน
      rotation: Number(data.rotation || 0),
      // ยังไม่มีรูปที่โหลด
      image: null,
    };
    // อัปเดตค่าต่าง ๆ ของสติกเกอร์ด้วยข้อมูลใหม่
    Object.assign(sticker, {
      // รูป
      src: data.src || sticker.src,
      // ตำแหน่ง
      surface: data.surface || sticker.surface,
      // ขนาด (ถ้าไม่ได้ส่งมาใช้ค่าเดิม)
      scale: Number(data.scale == null ? sticker.scale : data.scale),
      // มุมหมุน (ถ้าไม่ได้ส่งมาใช้ค่าเดิม)
      rotation: Number(data.rotation == null ? sticker.rotation : data.rotation),
    });
    // ถ้าเป็นของใหม่ เพิ่มลงลิสต์
    if (!existing) editor.stickers.push(sticker);
    // โหลดรูปให้เสร็จก่อน แล้วค่อยทำต่อ
    ensureStickerImage(sticker).then(() => {
      // เลือกสติกเกอร์นี้
      selectSticker(sticker.id);
      // วาดทุก material ใหม่
      rebuildAll();
      // ถ้าต้องแจ้ง Flutter
      if (shouldPost) {
        // แจ้งว่าวางสติกเกอร์แล้ว
        post('stickerPlaced', sticker.id, sticker.surface);
        // แจ้งว่าเลือกสติกเกอร์นี้
        post('stickerSelected', sticker.id, null);
      }
    });
  }

  // เริ่มเส้นใหม่
  function startStroke(target) {
    // สร้าง id เส้นจากเวลาปัจจุบันบวกเลขสุ่ม
    editor.strokeId = `${Date.now()}-${Math.floor(Math.random() * 100000)}`;
    // เข้ารหัสจุดแรก
    const surface = encodeTarget(target.materialIndex, target.u, target.v);
    // สร้างอ็อบเจ็กต์เส้น
    const stroke = {
      // id
      id: editor.strokeId,
      // รายการจุด เริ่มด้วยจุดแรก
      surfaces: [surface],
      // สีปัจจุบัน
      color: editor.color,
      // ขนาดปัจจุบัน
      size: editor.size,
      // ชนิดหัวปากกาปัจจุบัน
      brush: editor.brush,
    };
    // เพิ่มเส้นลงรายการ
    editor.strokes.push(stroke);
    // สั่งวาด material นี้ใหม่
    rebuildMaterial(target.materialIndex);
    // แจ้ง Flutter ว่าเริ่มเส้นใหม่
    post('strokeStarted', stroke.id, surface);
  }

  // ต่อจุดเข้ากับเส้นที่กำลังวาด
  function appendStroke(target) {
    // หาเส้นปัจจุบัน
    const stroke = editor.strokes.find((item) => item.id === editor.strokeId);
    // ถ้าไม่พบ ออก
    if (!stroke) return;
    // เข้ารหัสจุดใหม่
    const surface = encodeTarget(target.materialIndex, target.u, target.v);
    // ถอดรหัสจุดสุดท้ายเดิม
    const previous = decodeTarget(stroke.surfaces[stroke.surfaces.length - 1]);
    // ถ้าอยู่บน material เดียวกัน
    if (previous && previous.materialIndex === target.materialIndex) {
      // ผลต่าง u
      const du = previous.u - target.u;
      // ผลต่าง v
      const dv = previous.v - target.v;
      // ถ้าอยู่ใกล้กันเกินไป ข้ามเพื่อลดจำนวนจุด
      if (du * du + dv * dv < 0.000004) return;
    }
    // เพิ่มจุดลงเส้น
    stroke.surfaces.push(surface);
    // สั่งวาด material นี้ใหม่
    rebuildMaterial(target.materialIndex);
    // ถ้าข้ามไป material อื่น วาดตัวเดิมใหม่ด้วย
    if (previous && previous.materialIndex !== target.materialIndex) rebuildMaterial(previous.materialIndex);
    // แจ้ง Flutter ว่ามีจุดใหม่
    post('strokePoint', stroke.id, surface);
  }

  // ตอนนิ้วแตะลง
  renderer.domElement.addEventListener('pointerdown', (event) => {
    // ถ้าไม่ได้เปิดแก้ไขหรืออยู่โหมดสี ไม่ทำอะไร
    if (!editor.editable || editor.tool === 'color') return;
    // หาตำแหน่งผิวที่แตะ
    const target = hitAt(event);
    // ถ้าไม่โดนรองเท้า ออก
    if (!target) return;
    // ให้ canvas จับนิ้วต่อเนื่องแม้ลากออกนอกกรอบ
    renderer.domElement.setPointerCapture?.(event.pointerId);
    // ถ้าอยู่โหมดสติกเกอร์
    if (editor.tool === 'sticker') {
      // ถ้ามีสติกเกอร์รอวาง
      if (editor.pendingSticker) {
        // สร้างข้อมูลสติกเกอร์พร้อมตำแหน่งที่แตะ
        const data = Object.assign({}, editor.pendingSticker, {
          // ตำแหน่งที่แตะ
          surface: encodeTarget(target.materialIndex, target.u, target.v),
        });
        // ล้างสถานะรอวาง
        editor.pendingSticker = null;
        // เพิ่มสติกเกอร์และแจ้ง Flutter
        addSticker(data, true);
      // ถ้าไม่มีสติกเกอร์รอวาง
      } else {
        // หาสติกเกอร์ใกล้จุดที่แตะ
        const sticker = stickerNear(target);
        // เลือกสติกเกอร์นั้น (หรือยกเลิกเลือก) และแจ้ง Flutter
        selectSticker(sticker?.id || null, true);
        // เริ่มโหมดลากย้ายสติกเกอร์นี้
        editor.draggingStickerId = sticker?.id || null;
      }
    // ถ้าอยู่โหมดวาด
    } else if (editor.tool === 'draw') {
      // เริ่มสถานะกำลังวาด
      editor.drawing = true;
      // จำตำแหน่ง X ของนิ้ว
      editor.lastClientX = event.clientX;
      // จำตำแหน่ง Y ของนิ้ว
      editor.lastClientY = event.clientY;
      // เริ่มเส้นใหม่
      startStroke(target);
    }
    // กันไม่ให้หน้าเว็บเลื่อนหรือซูมเอง
    event.preventDefault();
  });

  // ตอนนิ้วเคลื่อนที่
  renderer.domElement.addEventListener('pointermove', (event) => {
    // ถ้าไม่ได้เปิดแก้ไข ไม่ทำอะไร
    if (!editor.editable) return;
    // ถ้ากำลังลากสติกเกอร์อยู่
    if (editor.draggingStickerId) {
      // หาตำแหน่งผิวใหม่ใต้นิ้ว
      const target = hitAt(event);
      // หาสติกเกอร์ที่กำลังลาก
      const sticker = editor.stickers.find((item) => item.id === editor.draggingStickerId);
      // ถ้าทั้งสองมี
      if (target && sticker) {
        // จำตำแหน่งเดิม
        const old = decodeTarget(sticker.surface);
        // ตั้งตำแหน่งใหม่
        sticker.surface = encodeTarget(target.materialIndex, target.u, target.v);
        // วาด material ใหม่ใหม่
        rebuildMaterial(target.materialIndex);
        // ถ้าย้ายข้าม material วาดตัวเดิมใหม่ด้วย
        if (old && old.materialIndex !== target.materialIndex) rebuildMaterial(old.materialIndex);
        // แจ้ง Flutter ว่าย้ายแล้ว
        post('stickerMoved', sticker.id, sticker.surface);
      }
      // กันหน้าเว็บเลื่อนเอง
      event.preventDefault();
    // ไม่เช่นนั้นถ้ากำลังวาดเส้นในโหมดวาด
    } else if (editor.drawing && editor.tool === 'draw') {
      // ระยะเคลื่อนที่แนว X จากครั้งก่อน
      const dx = event.clientX - editor.lastClientX;
      // ระยะเคลื่อนที่แนว Y จากครั้งก่อน
      const dy = event.clientY - editor.lastClientY;
      // ถ้าขยับน้อยกว่า 3 พิกเซล ข้าม (ลดจำนวนจุด)
      if (dx * dx + dy * dy < 9) return;
      // จำตำแหน่ง X
      editor.lastClientX = event.clientX;
      // จำตำแหน่ง Y
      editor.lastClientY = event.clientY;
      // หาตำแหน่งผิวใต้นิ้ว
      const target = hitAt(event);
      // ถ้าโดนรองเท้า ต่อจุดให้เส้น
      if (target) appendStroke(target);
      // กันหน้าเว็บเลื่อนเอง
      event.preventDefault();
    }
  });

  // ตอนยกนิ้วหรือถูกยกเลิก
  function finishPointer(event) {
    // หยุดวาด
    editor.drawing = false;
    // หยุดลากสติกเกอร์
    editor.draggingStickerId = null;
    // ล้าง id เส้นปัจจุบัน
    editor.strokeId = null;
    // ปล่อยการจับนิ้ว
    if (event?.pointerId != null) renderer.domElement.releasePointerCapture?.(event.pointerId);
  }
  // ผูก handler กับเหตุการณ์ยกนิ้ว
  renderer.domElement.addEventListener('pointerup', finishPointer);
  // ผูก handler กับเหตุการณ์ถูกยกเลิก
  renderer.domElement.addEventListener('pointercancel', finishPointer);

  // สร้างสถานะการวาดของ material หนึ่งตัว
  function createPaintState(material, index) {
    // สร้าง canvas สำหรับวาด
    const canvas = document.createElement('canvas');
    // กว้างตามขนาด texture
    canvas.width = TEXTURE_SIZE;
    // สูงตามขนาด texture
    canvas.height = TEXTURE_SIZE;
    // ขอ context 2D
    const ctx = canvas.getContext('2d', {willReadFrequently: false});
    // เก็บภาพ texture เดิมของ material (ถ้ามี)
    let originalCanvas = null;
    // ภาพต้นฉบับของ material
    const sourceImage = material.map?.image;
    // ถ้ามีภาพต้นฉบับที่ใช้ได้
    if (sourceImage?.width && sourceImage?.height) {
      // สร้าง canvas เก็บสำเนา
      originalCanvas = document.createElement('canvas');
      // กว้างตามขนาด texture
      originalCanvas.width = TEXTURE_SIZE;
      // สูงตามขนาด texture
      originalCanvas.height = TEXTURE_SIZE;
      // วาดภาพต้นฉบับลงในสำเนา
      originalCanvas.getContext('2d').drawImage(sourceImage, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE);
    }
    // สร้าง CanvasTexture จาก canvas (ผูกเข้ากับ GPU)
    const texture = new THREE.CanvasTexture(canvas);
    // ไม่พลิกแนวตั้งให้ตรงกับ UV ของไฟล์ GLB
    texture.flipY = false;
    // ใช้การเข้ารหัสสี sRGB
    texture.encoding = THREE.sRGBEncoding;
    // โหมดวนซ้ำแนวนอนตามภาพเดิม
    texture.wrapS = material.map?.wrapS || THREE.RepeatWrapping;
    // โหมดวนซ้ำแนวตั้งตามภาพเดิม
    texture.wrapT = material.map?.wrapT || THREE.RepeatWrapping;
    // ความคมเมื่อมองเฉียง สูงสุด 8 เท่า
    texture.anisotropy = Math.min(8, renderer.capabilities.getMaxAnisotropy());
    // เก็บสถานะ
    const state = {
      // เลข material
      index,
      // canvas ที่วาด
      canvas,
      // context 2D
      ctx,
      // texture ของ three.js
      texture,
      // ภาพต้นฉบับ
      originalCanvas,
      // สีปัจจุบัน ถ้ายังไม่เคยตั้งใช้สีเดิมของ material
      color: materialColors.get(index) || `#${material.color.getHexString()}`,
      // รายการ material ที่ใช้ texture นี้ร่วมกัน
      materials: [material],
    };
    // เปลี่ยนไปใช้ texture ของเราแทน
    material.map = texture;
    // ตั้งสี material เป็นขาว (สีจริงจะอยู่ใน texture แล้ว)
    material.color.set(0xffffff);
    // บอกให้ three.js คอมไพล์ material ใหม่
    material.needsUpdate = true;
    // เก็บสถานะลงแผนที่
    materialStates.set(index, state);
    // วาดภาพพื้นฐานครั้งแรก
    copyBaseImage(state);
    // บอกว่า texture เปลี่ยนแล้ว
    texture.needsUpdate = true;
    // คืนสถานะ
    return state;
  }

  // สร้างตัวโหลด GLB
  const loader = new GLTFLoader();
  // ตัวจัดการโหลดสำหรับตัวถอดรหัส Draco
  const decoderManager = new THREE.LoadingManager();
  // ให้คำขอไฟล์ตัวถอดรหัสทุกครั้งใช้ data URL ที่ Flutter ส่งมาแทน (ไม่ต้องขอเน็ต)
  decoderManager.setURLModifier(() => options.dracoDecoderUrl);
  // สร้างตัวโหลด Draco
  const draco = new DRACOLoader(decoderManager);
  // ใช้ตัวถอดรหัสแบบ JS ไม่ใช้ WASM
  draco.setDecoderConfig({type: 'js'});
  // ผูกตัวโหลด Draco เข้ากับตัวโหลด GLB
  loader.setDRACOLoader(draco);
  // โหลดโมเดลจากที่อยู่ที่ได้รับ
  loader.load(options.modelUrl, (gltf) => {
    // รากของฉากโมเดล
    const root = gltf.scene;
    // ไล่ดูทุกวัตถุในโมเดล
    root.traverse((object) => {
      // ข้ามถ้าไม่ใช่ mesh
      if (!object.isMesh) return;
      // ปิดการตัดวัตถุนอกกล้อง ป้องกันชิ้นส่วนหายตอนหมุน
      object.frustumCulled = false;
      // ทำให้ material เป็นอาร์เรย์เสมอ
      const materials = Array.isArray(object.material) ? object.material : [object.material];
      // วนทุก material
      for (const material of materials) {
        // หาข้อมูลว่า material นี้คือลำดับที่เท่าไรในไฟล์ GLB
        const association = gltf.parser.associations.get(material);
        // เลขลำดับ
        const index = association?.materials;
        // ถ้าไม่มีเลข ข้าม
        if (!Number.isInteger(index)) continue;
        // เก็บเลขไว้ใน userData เพื่อใช้ตอนยิงรังสี
        material.userData.gltfMaterialIndex = index;
        // ดูว่าลำดับนี้มีสถานะวาดแล้วหรือไม่
        const existing = materialStates.get(index);
        // ถ้ามีแล้ว (material ซ้ำกันหลายวัตถุ)
        if (existing) {
          // ใช้ texture ร่วมกัน
          material.map = existing.texture;
          // ตั้งสีขาว
          material.color.set(0xffffff);
          // เพิ่มเข้ารายการ material ที่ใช้ร่วมกัน
          existing.materials.push(material);
        // ถ้ายังไม่มี
        } else {
          // สร้างสถานะวาดใหม่
          createPaintState(material, index);
        }
      }
    });

    // ใส่โมเดลลงฉาก
    scene.add(root);
    // คำนวณกรอบล้อมของโมเดล
    const box = new THREE.Box3().setFromObject(root);
    // จุดศูนย์กลางของโมเดล
    const center = box.getCenter(new THREE.Vector3());
    // ขนาดของโมเดล
    const size = box.getSize(new THREE.Vector3());
    // ด้านที่ยาวที่สุด ใช้เป็นตัวแทนขนาด
    const radius = Math.max(size.x, size.y, size.z);
    // ให้กล้องหมุนรอบจุดศูนย์กลาง
    controls.target.copy(center);
    // วางกล้องในตำแหน่งเฉียงมองลงเล็กน้อย
    camera.position.set(center.x + radius * 1.7, center.y + radius * 0.65, center.z + radius * 1.9);
    // ระยะตัดใกล้ตามขนาดโมเดล
    camera.near = Math.max(0.001, radius / 1000);
    // ระยะตัดไกลตามขนาดโมเดล
    camera.far = radius * 100;
    // อัปเดตเมทริกซ์ของกล้อง
    camera.updateProjectionMatrix();
    // ซูมเข้าใกล้สุดตามขนาดโมเดล
    controls.minDistance = radius * 0.8;
    // ซูมออกไกลสุดตามขนาดโมเดล
    controls.maxDistance = radius * 5;
    // อัปเดตตัวควบคุมกล้อง
    controls.update();

    // ตั้งธงว่าโหลดเสร็จ
    editor.loaded = true;
    // วาด texture ทุก material
    rebuildAll();
    // ลบข้อความกำลังโหลด
    loading.remove();
    // ซ่อน model-viewer เดิมอย่างถาวร
    if (modelViewer) modelViewer.style.display = 'none';
    // แจ้ง Flutter ว่าพร้อมแล้ว (ช่อง ModelLoaded)
    if (window.ModelLoaded) window.ModelLoaded.postMessage('ready');
  // ไม่ใช้ callback ความคืบหน้า และถ้าโหลดล้มเหลว
  }, undefined, (error) => {
    // แสดงข้อความ โหลดโมเดล 3D ไม่สำเร็จ
    loading.textContent = 'โหลดโมเดล 3D ไม่สำเร็จ';
    // พิมพ์ error ลง console
    console.error(error);
  });

  // ปรับขนาดเมื่อหน้าต่างเปลี่ยน
  function resize() {
    // ความกว้างใหม่
    const width = window.innerWidth;
    // ความสูงใหม่
    const height = window.innerHeight;
    // อัปเดตอัตราส่วนกล้อง
    camera.aspect = width / Math.max(1, height);
    // อัปเดตเมทริกซ์กล้อง
    camera.updateProjectionMatrix();
    // อัปเดตขนาดตัวเรนเดอร์
    renderer.setSize(width, height);
  }
  // ผูกกับเหตุการณ์ resize ของหน้าต่าง
  window.addEventListener('resize', resize);

  // ลูปแสดงผลหลัก ทำงานทุกเฟรม: อัปเดตกล้อง วาด texture ที่เปลี่ยน แล้วเรนเดอร์
  function animate() {
    // ขอเฟรมถัดไปจากเบราว์เซอร์ (วนทำซ้ำทุกเฟรม)
    requestAnimationFrame(animate);
    // อัปเดตกล้อง (แรงเฉื่อย)
    controls.update();
    // วาด texture ใหม่เฉพาะ material ที่เปลี่ยน
    for (const index of dirtyMaterials) paintMaterial(index);
    // ล้างชุด material ที่ต้องวาดใหม่
    dirtyMaterials.clear();
    // เรนเดอร์ฉากลงจอ
    renderer.render(scene, camera);
  }
  // เริ่มวนลูปแสดงผล
  animate();

  // เปิดเมธอดให้ Flutter เรียกผ่าน window.shoePainter
  window.shoePainter = {
    // getter บอกว่าพร้อมใช้งานหรือยัง
    get isReady() {
      // คืนค่าธงโหลดเสร็จ
      return editor.loaded;
    },
    // ตั้งค่าตัวแก้ไข
    setConfig(config) {
      // ถ้าไม่มี config ออก
      if (!config) return;
      // จำเครื่องมือเดิม
      const previousTool = editor.tool;
      // ตั้งเครื่องมือใหม่
      editor.tool = config.tool || 'color';
      // ตั้งสีปากกา
      editor.color = parseHexColor(config.color || '#000000');
      // ตั้งขนาดปากกา
      editor.size = Number(config.size || 7);
      // ตั้งชนิดหัวปากกา
      editor.brush = config.brush || 'round';
      // ตั้งว่าแก้ไขได้หรือไม่
      editor.editable = !!config.editable;
      // เปิดการหมุนกล้องเฉพาะเมื่อไม่ได้อยู่ในโหมดแก้ไข หรืออยู่โหมดสี
      controls.enabled = !editor.editable || editor.tool === 'color';
      // ตั้งค่าการสัมผัสของ canvas (ทั้งสองกรณีได้ค่าเดียวกัน คือ none)
      renderer.domElement.style.touchAction = controls.enabled ? 'none' : 'none';
      // ถ้าเครื่องมือเปลี่ยน วาดใหม่เพื่อซ่อนหรือแสดงกรอบสติกเกอร์
      if (previousTool !== editor.tool) rebuildAll();
    },
    // ตั้งสีให้ material ตามรายการเลข
    setMaterialColor(indices, rgb) {
      // สร้างสีจากค่า R, G, B ช่วง 0 ถึง 1
      const color = new THREE.Color(Number(rgb[0]), Number(rgb[1]), Number(rgb[2]));
      // วนทุกเลข material
      for (const index of indices || []) {
        // แปลงเป็นตัวเลข
        const numericIndex = Number(index);
        // แปลงสีเป็นรหัสฐานสิบหก
        const hex = `#${color.getHexString()}`;
        // ถ้าสีเดิมเหมือนกัน ข้าม (ลดงานซ้ำ)
        if (materialColors.get(numericIndex) === hex) continue;
        // จำสีใหม่
        materialColors.set(numericIndex, hex);
        // ดึงสถานะวาดของ material
        const state = materialStates.get(numericIndex);
        // ถ้ายังไม่มี (โมเดลยังไม่โหลด) ข้าม สีจะถูกใช้ตอนสร้างสถานะ
        if (!state) continue;
        // ตั้งสีในสถานะ
        state.color = hex;
        // สั่งวาดใหม่
        rebuildMaterial(numericIndex);
      }
    },
    // ตั้งสติกเกอร์ที่รอผู้ใช้แตะวาง
    setPendingSticker(data) {
      // เก็บข้อมูล (null คือยกเลิก)
      editor.pendingSticker = data || null;
    },
    // เลือกสติกเกอร์จาก Flutter (ไม่แจ้งกลับ)
    selectSticker(id) {
      // เรียกฟังก์ชันภายใน
      selectSticker(id, false);
    },
    // อัปเดตสติกเกอร์จาก Flutter
    updateSticker(data) {
      // หาสติกเกอร์ตาม id
      const sticker = data && editor.stickers.find((item) => item.id === String(data.id));
      // ถ้าไม่พบ ออก
      if (!sticker) return;
      // ตำแหน่งเดิม
      const old = decodeTarget(sticker.surface);
      // ถ้ามีตำแหน่งใหม่ ตั้งค่า
      if (data.surface) sticker.surface = data.surface;
      // ถ้ามีขนาดใหม่ ตั้งค่า
      if (data.scale != null) sticker.scale = Number(data.scale);
      // ถ้ามีมุมใหม่ ตั้งค่า
      if (data.rotation != null) sticker.rotation = Number(data.rotation);
      // ตำแหน่งใหม่
      const next = decodeTarget(sticker.surface);
      // วาด material เดิมใหม่
      if (old) rebuildMaterial(old.materialIndex);
      // ถ้าย้ายข้าม material วาดตัวใหม่ด้วย
      if (next && (!old || next.materialIndex !== old.materialIndex)) rebuildMaterial(next.materialIndex);
    },
    // ลบสติกเกอร์ตาม id
    removeSticker(id) {
      // หาสติกเกอร์ที่จะลบ
      const removed = editor.stickers.find((item) => item.id === String(id));
      // กรองชิ้นนั้นออกจากลิสต์
      editor.stickers = editor.stickers.filter((item) => item.id !== String(id));
      // ถ้าเป็นชิ้นที่เลือกอยู่ ยกเลิกการเลือก
      if (editor.selectedStickerId === String(id)) editor.selectedStickerId = null;
      // ตำแหน่งของชิ้นที่ลบ
      const target = removed && decodeTarget(removed.surface);
      // วาด material นั้นใหม่
      if (target) rebuildMaterial(target.materialIndex);
    },
    // ลบเส้นตาม id
    removeStroke(id) {
      // หาเส้นที่จะลบ
      const removed = editor.strokes.find((item) => item.id === String(id));
      // กรองเส้นนั้นออกจากลิสต์
      editor.strokes = editor.strokes.filter((item) => item.id !== String(id));
      // รวบรวมเลข material ทั้งหมดที่เส้นนั้นผ่าน (ไม่ซ้ำ)
      const materials = new Set((removed?.surfaces || []).map(decodeTarget).filter(Boolean).map((p) => p.materialIndex));
      // สั่งวาดทุก material เหล่านั้นใหม่
      materials.forEach(rebuildMaterial);
    },
    // ล้างลายวาดทั้งหมด
    clearDrawing() {
      // ล้างรายการเส้น
      editor.strokes = [];
      // วาดทุก material ใหม่
      rebuildAll();
    },
    // วาดสติกเกอร์และเส้นทั้งหมดจากข้อมูลที่ Flutter ส่งมา (ใช้ตอนเริ่ม WebView ใหม่)
    renderAll(payload) {
      // ตั้งรายการเส้นจากข้อมูลที่ส่งมา (ทำสำเนาอาร์เรย์จุด)
      editor.strokes = (payload?.strokes || []).map((stroke) => ({...stroke, surfaces: [...(stroke.surfaces || [])]}));
      // ตั้งรายการสติกเกอร์จากข้อมูลที่ส่งมา (ยังไม่มีรูปที่โหลด)
      editor.stickers = (payload?.stickers || []).map((sticker) => ({...sticker, id: String(sticker.id), image: null}));
      // โหลดรูปสติกเกอร์ทุกชิ้นให้เสร็จ แล้วค่อยวาดใหม่ทั้งหมด
      Promise.all(editor.stickers.map(ensureStickerImage)).then(rebuildAll);
    },
  };
}

// เปิดฟังก์ชัน startShoePainter ให้ Flutter เรียกผ่าน window
window.startShoePainter = startShoePainter;
