﻿using Engine.Compression;
using Engine.DataModels;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Engine.Managers.Graphics;

/// <summary>
/// Handles the work of reading the base files and loading those into a basic format to be consumed
/// </summary>
public class VgaGraphicsManager : IGraphicsManager
{
    private static volatile VgaGraphicsManager? _instance = null;
    private static object syncRoot = new object();

    // TODO: Turn this into an object that can be read by other things
    private byte[][] grsegs = new byte[Constants.NumChunks][];
    private pictabletype[] pictable = new pictabletype[Constants.NumPics];

    private VgaGraphicsManager()
    {
    }

    public static VgaGraphicsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // only create a new instance if one doesn't already exist.
                lock (syncRoot)
                {
                    // use this lock to ensure that only one thread can access
                    // this block of code at once.
                    if (_instance == null)
                    {
                        _instance = new VgaGraphicsManager();
                    }
                }
            }

            return _instance;
        }
    }

    public Graphic GetGraphic(string name)
    {
        var chunkNum = LookupChunkByName(name);
        var picdata = pictable[chunkNum - Constants.StartPics];

        return new Graphic
        {
            Data = grsegs[chunkNum],
            Width = (ushort)picdata.width,
            Height = (ushort)picdata.height
        };
    }

    private int LookupChunkByName(string name)
    {
        if (!GraphicNameChunkMapping.TryGetValue(name.ToLowerInvariant(), out var mapping))
            throw new KeyNotFoundException(name);

        return mapping;
    }

    private struct pictabletype
    {
        public short width, height;
    }

    public void LoadDataFiles()
    {
        // Try loading the VGA files
        const string fileExtension = "wl6";

        const string tempDirectory = "D:\\PFWolf-CSharp\\";

        const string graphicsFileName = "vgagraph";
        const string graphicsHeaderFileName = "vgahead";
        const string graphicsDictionaryFileName = "vgadict";

        // Load dictionary
        byte[] dictionaryFile = File.ReadAllBytes($"{tempDirectory}{graphicsDictionaryFileName}.{fileExtension}");


        // Load header
        var headerLength = new FileInfo($"{tempDirectory}{graphicsHeaderFileName}.{fileExtension}").Length;

        const int magic3 = 3;

        var grstarts = new int[Constants.NumChunks + 1];
        var expectedSize = grstarts.Length; // NUMCHUNKS

        if (headerLength / magic3 != expectedSize) // What does the 3 mean? Why 3?
        {
            throw new Exception("VGA File is not right");
        }

        byte[] headerFile = File.ReadAllBytes($"{tempDirectory}{graphicsHeaderFileName}.{fileExtension}");

        for (var i = 0; i != grstarts.Length; i++)
        {
            var d0 = headerFile[i * magic3];
            var d1 = headerFile[i * magic3 + 1];
            var d2 = headerFile[i * magic3 + 2];
            int val = d0 | d1 << 8 | d2 << 16;
            grstarts[i] = val == 0x00FFFFFF ? -1 : val;
        }

        // Expand and Load graphics
        byte[] graphicsFile = File.ReadAllBytes($"{tempDirectory}{graphicsFileName}.{fileExtension}");

        var filePos = grstarts[Constants.StructPic];
        int chunkcomplen;//, chunkexplen;

        //chunkexplen = BitConverter.ToInt32(graphicsFile.Skip(filePos).Take(4).ToArray(), 0);
        chunkcomplen = grstarts[Constants.StructPic + 1] - grstarts[Constants.StructPic] - 4;
        var compseg = graphicsFile.Skip(filePos + 4).Take(chunkcomplen).ToArray();


        var pictableSize = Marshal.SizeOf(typeof(pictabletype)) * pictable.Length;

        var compression = new HuffmanCompression(dictionaryFile);
        var destTable = compression.Expand(compseg, pictableSize);

        pictable = ByteArrayToStuctureArray<pictabletype>(destTable, pictable.Length);
        //free(compseg);

        CA_CacheGrChunks(grstarts, grsegs, graphicsFile, compression);

    }

    private void CA_CacheGrChunks(int[] grstarts, byte[][] grsegs, byte[] graphicsFile, ICompression compression)
    {
        int pos, compressed;
        //int* bufferseg;
        //int* source;
        int chunk, next;

        for (chunk = Constants.StructPic + 1; chunk < Constants.NumChunks; chunk++)
        {
            // If there is data in the grsegs chunk, skip it
            if (grsegs[chunk] != null && grsegs[chunk].Length > 0)
                continue;                             // already in memory

            //
            // load the chunk into a buffer
            //
            pos = grstarts[chunk];

            if (pos < 0)                              // $FFFFFFFF start is a sparse tile
                continue;

            next = chunk + 1;

            while (grstarts[next] == -1)           // skip past any sparse tiles
                next++;

            compressed = grstarts[next] - pos; // TODO: I'm off by 4 each time

            // load into source, an int32 worth of byte data (so 4?)
            //var source = BitConverter.ToInt32(graphicsFile.Skip(pos).Take(4).ToArray(), 0);
            var source = graphicsFile.Skip(pos).Take(compressed).ToArray();

            var bufferseg = new int[compressed];// SafeMalloc(compressed);
            //source = bufferseg;

            //read(grhandle, source, compressed);

            CAL_ExpandGrChunk(chunk, source, compression);

            if (chunk >= Constants.StartPics && chunk < Constants.StartExterns)
                CAL_DeplaneGrChunk(chunk);

            //free(bufferseg);
        }
    }
    private void CAL_ExpandGrChunk(int chunk, byte[] source, ICompression compression)
    {
        int expanded;
        int sourceIndex = 0;

        if (chunk >= Constants.StartTile8s && chunk < Constants.StartExterns)
        {
            //
            // expanded sizes of tile8/16/32 are implicit
            //

            const int BLOCK = 64;
            const int MASKBLOCK = 128;

            //if (chunk < STARTTILE8M)          // tile 8s are all in one chunk!
            //    expanded = BLOCK * NUMTILE8;
            //else if (chunk < STARTTILE16)
            //    expanded = MASKBLOCK * NUMTILE8M;
            //else if (chunk < STARTTILE16M)    // all other tiles are one/chunk
            //    expanded = BLOCK * 4;
            //else if (chunk < STARTTILE32)
            //    expanded = MASKBLOCK * 4;
            //else if (chunk < STARTTILE32M)
            //    expanded = BLOCK * 16;
            //else
            expanded = MASKBLOCK * 16;
        }
        else
        {
            //
            // everything else has an explicit size longword
            //
            expanded = BitConverter.ToInt32(source.Skip(sourceIndex++).Take(4).ToArray()); // TODO: This needs 4 bytes into expanded
        }

        //
        // allocate final space and decompress it
        //
        //grsegs[chunk] = new byte[expanded];// SafeMalloc(expanded);

        grsegs[chunk] = compression.Expand(source.Skip(4).ToArray(), expanded);
    }
    private void CAL_DeplaneGrChunk(int chunk)
    {
        int i;
        short width, height;

        if (chunk == Constants.StartTile8s)
        {
            width = height = 8;

            for (i = 0; i < Constants.NumTile8; i++)
                VL_DePlaneVGA(grsegs[chunk]/*[(i * (width * height))]*/, width, height);
        }
        else
        {
            width = pictable[chunk - Constants.StartPics].width;
            height = pictable[chunk - Constants.StartPics].height;

            VL_DePlaneVGA(grsegs[chunk], width, height);
        }
    }
    private void VL_DePlaneVGA(byte[] source, int width, int height)
    {
        int x, y, plane;
        ushort size, pwidth;
        byte[] temp, dest, srcline;

        size = (ushort)(width * height);

        if ((width & 3) != 0)
            throw new Exception("DePlaneVGA: width not divisible by 4!");

        temp = new byte[size];// SafeMalloc(size);

        //
        // munge pic into the temp buffer
        //
        srcline = source;
        var srcLineIndex = 0;
        pwidth = (ushort)(width >> 2);

        for (plane = 0; plane < 4; plane++)
        {
            dest = temp;

            for (y = 0; y < height; y++)
            {
                for (x = 0; x < pwidth; x++)
                    dest[width * y + (x << 2) + plane] = srcline[srcLineIndex++];
            }
        }

        //
        // copy the temp buffer back into the original source
        //
        Array.Copy(temp, source, size);
    }

    private static T[] ByteArrayToStuctureArray<T>(byte[] bytes, int lengthOfT) where T : struct
    {
        var outT = new T[lengthOfT];
        var grHuffmanSize = Marshal.SizeOf(typeof(T)) * lengthOfT;
        int i, j;
        for (i = 0, j = 0; i < grHuffmanSize; j++, i += Marshal.SizeOf(typeof(T)))
        {
            var huffBytes = bytes.Skip(i).Take(Marshal.SizeOf(typeof(T))).ToArray();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            try
            {
                Marshal.Copy(huffBytes, 0, ptr, Marshal.SizeOf(typeof(T)));
                outT[j] = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        return outT;
    }

    private Dictionary<string, int> GraphicNameChunkMapping = new Dictionary<string, int>
    {
        { "readthis/bj",3 },
        { "readthis/castle", 4 },
        { "readthis/blaze", 5 },
        { "readthis/topwindow", 6 },
        { "readthis/leftwindow", 7 },
        { "readthis/rightwindow", 8 },
        { "readthis/bottominfo", 9 },
        { "menus/options", 10 },
        { "menus/cursor1", 11 },
        { "menus/cursor2", 12 },
        { "menus/notselected", 13 },
        { "menus/selected", 14 },
        { "menus/fxtitle", 15 },
        { "menus/digititle", 16 },
        { "menus/musictitle", 17 },
        { "menus/mouselback", 18 },
        { "menus/babymode", 19 },
        { "menus/easymode", 20 },
        { "menus/normalmode", 21 },
        { "menus/hardmode", 22 },
        { "menus/loadsavedisk", 23 },
        { "menus/diskloading1", 24 },
        { "menus/diskloading2", 25 },
        { "menus/control", 26 },
        { "menus/customize", 27 },
        { "menus/loadgame", 28 },
        { "menus/savegame", 29 },
        { "menus/episode1", 30 },
        { "menus/episode2", 31 },
        { "menus/episode3", 32 },
        { "menus/episode4", 33 },
        { "menus/episode5", 34 },
        { "menus/episode6", 35 },
        { "menus/code", 36 },
        { "menus/timecode", 37 },
        { "menus/level", 38 },
        { "menus/name", 39 },
        { "menus/score", 40 },
        { "menus/joy1", 41 },
        { "menus/joy2", 42 },
        { "intermission/guy", 43 },
        { "intermission/colon", 44 },
        { "intermission/num0", 45 },
        { "intermission/num1", 46 },
        { "intermission/num2", 47 },
        { "intermission/num3", 48 },
        { "intermission/num4", 49 },
        { "intermission/num5", 50 },
        { "intermission/num6", 51 },
        { "intermission/num7", 52 },
        { "intermission/num8", 53 },
        { "intermission/num9", 54 },
        { "intermission/percent", 55 },
        { "intermission/a", 56 },
        { "intermission/b", 57 },
        { "intermission/c", 58 },
        { "intermission/d", 59 },
        { "intermission/e", 60 },
        { "intermission/f", 61 },
        { "intermission/g", 62 },
        { "intermission/h", 63 },
        { "intermission/i", 64 },
        { "intermission/j", 65 },
        { "intermission/k", 66 },
        { "intermission/l", 67 },
        { "intermission/m", 68 },
        { "intermission/n", 69 },
        { "intermission/o", 70 },
        { "intermission/p", 71 },
        { "intermission/q", 72 },
        { "intermission/r", 73 },
        { "intermission/s", 74 },
        { "intermission/t", 75 },
        { "intermission/u", 76 },
        { "intermission/v", 77 },
        { "intermission/w", 78 },
        { "intermission/x", 79 },
        { "intermission/y", 80 },
        { "intermission/z", 81 },
        { "intermission/expoint", 82 },
        { "intermission/apostrophe", 83 },
        { "intermission/guy2", 84 },
        { "intermission/bjwins", 85 },
        { "hud/statusbar", 86 },
        { "title", 87 },
        { "pg13", 88 },
        { "credits", 89 },
        { "highscores", 90 },
        { "hud/knife", 91 },
        { "hud/pistol", 92 },
        { "hud/machinegun", 93 },
        { "hud/chaingun", 94 },
        { "hud/nokey", 95 },
        { "hud/goldkey", 96 },
        { "hud/silverkey", 97 },
        { "hud/num_blank", 98 },
        { "hud/num0", 99 },
        { "hud/num1", 100 },
        { "hud/num2", 101 },
        { "hud/num3", 102 },
        { "hud/num4", 103 },
        { "hud/num5", 104 },
        { "hud/num6", 105 },
        { "hud/num7", 106 },
        { "hud/num8", 107 },
        { "hud/num9", 108 },
        { "hud/face1a", 109 },
        { "hud/face1b", 110 },
        { "hud/face1c", 111 },
        { "hud/face2a", 112 },
        { "hud/face2b", 113 },
        { "hud/face2c", 114 },
        { "hud/face3a", 115 },
        { "hud/face3b", 116 },
        { "hud/face3c", 117 },
        { "hud/face4a", 118 },
        { "hud/face4b", 119 },
        { "hud/face4c", 120 },
        { "hud/face5a", 121 },
        { "hud/face5b", 122 },
        { "hud/face5c", 123 },
        { "hud/face6a", 124 },
        { "hud/face6b", 125 },
        { "hud/face6c", 126 },
        { "hud/face7a", 127 },
        { "hud/face7b", 128 },
        { "hud/face7c", 129 },
        { "hud/face8a", 130 },
        { "hud/face_gotgatling", 131 },
        { "hud/face_mutant", 132 },
        { "paused", 133 },
        { "getpsyched", 134 }
    };
}
