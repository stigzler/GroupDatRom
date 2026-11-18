using System;
using System.Collections.Generic;
using System.Text;

namespace GroupDatRom.Models
{
    internal enum NodeType
    {
        Root,
        FolderFull,
        FolderEmpty,
        FolderNew,

        // Playlists & Track Layouts
        FileM3u,
        FileCue,
        FileGdi,          // Dreamcast track index files (.gdi)


        // Processed & Compressed Optical Outputs (Compressatorium targets)
        FileChd,          // PS1, PS2, Saturn, Dreamcast (.chd) : wooden-box
        FileRvzWia,       // GameCube / Wii (.rvz, .wia, .gcz) : drawer
        FileCsoZso,       // PSP / Handheld (.cso, .zso, .dax) : briefcase
        File3DsContainer, // 3DS formats (.3ds, .z3ds, etc.) : luggage
        FileStandardIso,  // Generic single-file ISOs / WBFS / WUX : disc
        FileBin,          // bin files


        // Classic Cartridge ROMs (NES, SNES, Genesis, GBA, N64, etc.)
        FileCartridge,    // .nes, .sfc, .md, .gb, .gba, .n64, etc. : cassette


        // Vintage Disks, Tapes & Media (Atari 800, C64, Amiga) : 
        FileVintageMedia, // .atr, .dsk, .cas, .d64, .adf, .st : disk


        // Companion Assets (Art, Manuals, Soundtracks)
        FileImage,        // .png, .jpg, .jpeg, .webp, .bmp (Box art, snapshots): image-sunset
        FileMusic,        // .mp3, .flac, .ogg, .wav, .aac (Soundtracks, background audio) : music-beam


        // Unprocessed / Raw Inputs
        Archive,          // .zip, .7z, .rar (Needs extraction/handling): box
        FileOther         // Readmes, logs, unknown files: blue-document
    }
}
