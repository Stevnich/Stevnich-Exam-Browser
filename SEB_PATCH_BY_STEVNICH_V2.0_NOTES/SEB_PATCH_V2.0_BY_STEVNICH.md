Ringkasan: Patch ini menambahkan mode debugging dan fitur dummy proctor, serta melakukan penyeragaman warna ikon sistem untuk konsistensi visual dan keamanan.


<h1 style="font-family: consolas;">Hal yang Ditambahkan</h1>

- ### Debugging Mode Switch
    - **Keybind: Host + L**
    - Peralihan antara dua mode:
        - *Normal*
            - Perilaku seperti SEB standar: keybind Windows dinonaktifkan, Alt+Tab ditangani oleh SEB, dan tampilan beralih ke mode kiosk dengan latar hitam.
        - *Debug*
            - Menonaktifkan semua mekanisme penguncian SEB: mode kiosk dimatikan sehingga desktop biasa akan tampak.
            - Semua kombinasi tombol Windows berfungsi kembali, termasuk Alt+Tab.
            - SEB sepenuhnya menonaktifkan handler keybind internal (MATIKAN TOTAL HANDLER KEYBIND DARI SEB).
            - Semua fungsi windows button (win + tab, win + D, dll.)
- ### Dummy Proctor System
    - Jika konfigurasi mengharuskan sesi proctor tetapi fitur proctoring tidak aktif, ikon dan peringatan proctor tetap ditampilkan secara visual.
    - Namun, tampilan tersebut bersifat dummy, tidak ada fungsi proctoring yang dijalankan (hanya tampilan ikon/peringatan tanpa aksi).


<h1 style="font-family: consolas;">Perubahan (Modifikasi)</h1>

- ### Penyeragaman Warna Ikon Sistem
    - Semua ikon sistem (system icons) dikembalikan ke warna hitam (#000000).
    - Tujuan: menjaga konsistensi visual terhadap latar aplikasi dan mengurangi risiko informasi visual yang mudah terlihat atau membingungkan pengguna; juga meningkatkan aspek keamanan tampilan.
    
- ### Tema Gelap akan dihilangkan
	-  Pengembangan Tema Gelap akan dihentikan demi keselarasan dengan antarmuka lainnya dan  kestabilan antarmuka sistem.
	
- ### Jendela Companion dihentikan
	- Untuk mengurangi jumlah resource yang digunakan SEB saat program berjalan, Companion akan dihilangkan.
	
- ### Pendeteksi VM dihilangkan
	- Safe Exam Browser tidak lagi mencoba mendeteksi lingkungan mesin virtual saat anda menjalankan SEB di mesin virtual, meskipun konfigurasi "***Allow to run inside Virtual Machine***" dimatikan.