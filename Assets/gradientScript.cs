using UnityEngine;
using System;
using System.Collections.Generic;

public sealed class Maze3D
{
    public const int N = 9;
    public const int TrapsPerLayer = 2;

    // Направления: 0:+x 1:-x 2:+y 3:-y 4:+z 5:-z (противоположное = d ^ 1)
    static readonly int[] DX  = { 1, -1, 0, 0 };
    static readonly int[] DY  = { 0, 0, 1, -1 };
    static readonly int[] OFF = { 1, -1, N, -N, N * N, -N * N };

    public readonly byte[] Open = new byte[N * N * N]; // бит d = проход в направлении d
    public readonly bool[] Trap = new bool[N * N * N];
    public readonly int[] ConnX = new int[N - 1];
    public readonly int[] ConnY = new int[N - 1];
    public int Start, End;                              // индекс = z*81 + y*9 + x
    public List<int> Path = new List<int>();            // от старта до финиша

    uint state;

    public static uint Rand(uint seed)
    {
        unchecked
        {
            uint x = seed + 0x9E3779B9u;
            x ^= x >> 16; x *= 0x7FEB352Du;
            x ^= x >> 15; x *= 0x846CA68Bu;
            x ^= x >> 16;
            return x;
        }
    }

    uint Next() { uint r = Rand(state); state = unchecked(state + 1); return r; }
    int NextInt(int n) => (int)(Next() % (uint)n);

    bool IsConnector(int i)
    {
        int x = i % N, y = i / N % N, z = i / (N * N);
        if (z < N - 1 && ConnX[z] == x && ConnY[z] == y) return true;
        if (z > 0 && ConnX[z - 1] == x && ConnY[z - 1] == y) return true;
        return false;
    }

    int[] Perm()
    {
        var a = new int[N];
        for (int i = 0; i < N; i++) a[i] = i;
        for (int i = N - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            int t = a[i]; a[i] = a[j]; a[j] = t;
        }
        return a;
    }

    int Coord(int bit)
    {
        int r = NextInt(3);
        return bit == 0 ? r : 6 + r;
    }

    int PickCell(int oct)
    {
        while (true)
        {
            int x = Coord(oct & 1);
            int y = Coord((oct >> 1) & 1);
            int z = Coord((oct >> 2) & 1);
            int i = (z * N + y) * N + x;
            if (!IsConnector(i)) return i;
        }
    }

    public Maze3D(uint seed)
    {
        state = Rand(seed);

        // 1. Координаты переходов: все x различны, все y различны
        int[] px = Perm(), py = Perm();
        for (int k = 0; k < N - 1; k++) { ConnX[k] = px[k]; ConnY[k] = py[k]; }

        // 2. Слои: DFS-лабиринт
        var visited = new bool[N * N];
        var stack = new int[N * N];
        var cand = new int[4];
        for (int z = 0; z < N; z++)
        {
            Array.Clear(visited, 0, visited.Length);
            int sp = 0;
            int sx = NextInt(N), sy = NextInt(N);
            visited[sy * N + sx] = true;
            stack[sp++] = sy * N + sx;
            while (sp > 0)
            {
                int c = stack[sp - 1];
                int cx = c % N, cy = c / N, cnt = 0;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + DX[d], ny = cy + DY[d];
                    if (nx >= 0 && nx < N && ny >= 0 && ny < N && !visited[ny * N + nx])
                        cand[cnt++] = d;
                }
                if (cnt == 0) { sp--; continue; }
                int dir = cand[NextInt(cnt)];
                int mx = cx + DX[dir], my = cy + DY[dir];
                Open[(z * N + cy) * N + cx] |= (byte)(1 << dir);
                Open[(z * N + my) * N + mx] |= (byte)(1 << (dir ^ 1));
                visited[my * N + mx] = true;
                stack[sp++] = my * N + mx;
            }
        }

        // 3. Переходы между слоями
        for (int k = 0; k < N - 1; k++)
        {
            Open[(k * N + ConnY[k]) * N + ConnX[k]]       |= 1 << 4;
            Open[((k + 1) * N + ConnY[k]) * N + ConnX[k]] |= 1 << 5;
        }

        // 4. Старт и финиш в противоположных октантах
        int oct = NextInt(8);
        Start = PickCell(oct);
        End = PickCell(7 - oct);

        // 5. Путь (BFS; в дереве он единственный)
        int V = N * N * N;
        var parent = new int[V];
        Array.Fill(parent, -1);
        var queue = new int[V];
        int qh = 0, qt = 0;
        parent[Start] = Start;
        queue[qt++] = Start;
        while (qh < qt)
        {
            int c = queue[qh++];
            for (int d = 0; d < 6; d++)
            {
                if ((Open[c] >> d & 1) == 0) continue;
                int n = c + OFF[d];
                if (parent[n] != -1) continue;
                parent[n] = c;
                queue[qt++] = n;
            }
        }
        for (int c = End; ; c = parent[c])
        {
            Path.Add(c);
            if (c == Start) break;
        }
        Path.Reverse();

        // 6. Ловушки: одна гарантированно на пути
        var onPath = new List<int>();
        foreach (int c in Path)
            if (c != Start && c != End && !IsConnector(c)) onPath.Add(c);
        Trap[onPath[NextInt(onPath.Count)]] = true;

        for (int z = 0; z < N; z++)
        {
            int count = 0;
            for (int i = 0; i < N * N; i++) if (Trap[z * N * N + i]) count++;
            while (count < TrapsPerLayer)
            {
                int i = z * N * N + NextInt(N * N);
                if (i == Start || i == End || Trap[i] || IsConnector(i)) continue;
                Trap[i] = true;
                count++;
            }
        }
    }
}

public class gradientScript : MonoBehaviour{

}