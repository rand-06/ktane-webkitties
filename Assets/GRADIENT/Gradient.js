const N = 9;
const TRAPS_PER_LAYER = 2;
const DX = [1, -1, 0, 0], DY = [0, 0, 1, -1];
const OFF = [1, -1, N, -N, N * N, -N * N];

function rand(seed) {
  let x = (seed + 0x9E3779B9) >>> 0;
  x ^= x >>> 16;
  x = Math.imul(x, 0x7FEB352D);
  x ^= x >>> 15;
  x = Math.imul(x, 0x846CA68B);
  x ^= x >>> 16;
  return x >>> 0;
}

function generateMaze(seed) {
  let state = rand(seed >>> 0);
  const next = () => { const r = rand(state); state = (state + 1) >>> 0; return r; };
  const nextInt = n => next() % n;

  const open = new Uint8Array(N * N * N);   // бит d = проход в направлении d
  const trap = new Uint8Array(N * N * N);
  const connX = [], connY = [];

  const isConnector = i => {
    const x = i % N, y = Math.floor(i / N) % N, z = Math.floor(i / (N * N));
    if (z < N - 1 && connX[z] === x && connY[z] === y) return true;
    if (z > 0 && connX[z - 1] === x && connY[z - 1] === y) return true;
    return false;
  };

  const perm = () => {
    const a = Array.from({ length: N }, (_, i) => i);
    for (let i = N - 1; i > 0; i--) {
      const j = nextInt(i + 1);
      const t = a[i]; a[i] = a[j]; a[j] = t;
    }
    return a;
  };

  // 1. Координаты переходов: все x различны, все y различны
  const px = perm(), py = perm();
  for (let k = 0; k < N - 1; k++) { connX.push(px[k]); connY.push(py[k]); }

  // 2. Слои: DFS-лабиринт
  const stack = new Int32Array(N * N);
  const cand = [0, 0, 0, 0];
  for (let z = 0; z < N; z++) {
    const visited = new Uint8Array(N * N);
    let sp = 0;
    const sx = nextInt(N), sy = nextInt(N);
    visited[sy * N + sx] = 1;
    stack[sp++] = sy * N + sx;
    while (sp > 0) {
      const c = stack[sp - 1];
      const cx = c % N, cy = Math.floor(c / N);
      let cnt = 0;
      for (let d = 0; d < 4; d++) {
        const nx = cx + DX[d], ny = cy + DY[d];
        if (nx >= 0 && nx < N && ny >= 0 && ny < N && !visited[ny * N + nx]) cand[cnt++] = d;
      }
      if (cnt === 0) { sp--; continue; }
      const dir = cand[nextInt(cnt)];
      const mx = cx + DX[dir], my = cy + DY[dir];
      open[(z * N + cy) * N + cx] |= 1 << dir;
      open[(z * N + my) * N + mx] |= 1 << (dir ^ 1);
      visited[my * N + mx] = 1;
      stack[sp++] = my * N + mx;
    }
  }

  // 3. Переходы между слоями
  for (let k = 0; k < N - 1; k++) {
    open[(k * N + connY[k]) * N + connX[k]] |= 1 << 4;
    open[((k + 1) * N + connY[k]) * N + connX[k]] |= 1 << 5;
  }

  // 4. Старт и финиш в противоположных октантах
  const coord = bit => { const r = nextInt(3); return bit === 0 ? r : 6 + r; };
  const pickCell = oct => {
    for (;;) {
      const x = coord(oct & 1);
      const y = coord((oct >> 1) & 1);
      const z = coord((oct >> 2) & 1);
      const i = (z * N + y) * N + x;
      if (!isConnector(i)) return i;
    }
  };
  const oct = nextInt(8);
  const start = pickCell(oct);
  const end = pickCell(7 - oct);

  // 5. Путь (BFS; в дереве он единственный)
  const V = N * N * N;
  const parent = new Int32Array(V).fill(-1);
  const queue = new Int32Array(V);
  let qh = 0, qt = 0;
  parent[start] = start;
  queue[qt++] = start;
  while (qh < qt) {
    const c = queue[qh++];
    for (let d = 0; d < 6; d++) {
      if (((open[c] >> d) & 1) === 0) continue;
      const n = c + OFF[d];
      if (parent[n] !== -1) continue;
      parent[n] = c;
      queue[qt++] = n;
    }
  }
  const path = [];
  for (let c = end; ; c = parent[c]) {
    path.push(c);
    if (c === start) break;
  }
  path.reverse();

  // 6. Ловушки: одна гарантированно на пути
  const onPath = path.filter(c => c !== start && c !== end && !isConnector(c));
  trap[onPath[nextInt(onPath.length)]] = 1;

  for (let z = 0; z < N; z++) {
    let count = 0;
    for (let i = 0; i < N * N; i++) if (trap[z * N * N + i]) count++;
    while (count < TRAPS_PER_LAYER) {
      const i = z * N * N + nextInt(N * N);
      if (i === start || i === end || trap[i] || isConnector(i)) continue;
      trap[i] = 1;
      count++;
    }
  }

  return { open, trap, connX, connY, start, end, path };
}