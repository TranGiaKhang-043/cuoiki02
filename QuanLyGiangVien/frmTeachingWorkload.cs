using System;
using System.Linq;
using System.Windows.Forms;

namespace QuanLyGiangVien
{
    public partial class frmTeachingWorkload : Form
    {
        private const int DinhMucTietMoiHocKy = 300;
        private bool dangNapBoLoc;

        public frmTeachingWorkload()
        {
            InitializeComponent();
            Load += frmTeachingWorkload_Load;
            btnThongKe.Click += btnThongKe_Click;
            btnLamMoi.Click += btnLamMoi_Click;
        }

        private void frmTeachingWorkload_Load(object sender, EventArgs e)
        {
            if (LicenseManagerIsDesignTime()) return;

            try
            {
                using (var db = new QuanLyGiangVienDataContext())
                {
                    dangNapBoLoc = true;

                    cboNamHoc.DataSource = db.NAM_HOCs
                        .OrderByDescending(x => x.TuNam)
                        .Select(x => new { x.MaNamHoc, x.TenNamHoc })
                        .ToList();
                    cboNamHoc.DisplayMember = "TenNamHoc";
                    cboNamHoc.ValueMember = "MaNamHoc";

                    cboHocKy.DataSource = db.HOC_Kies
                        .OrderBy(x => x.MaHocKy)
                        .Select(x => new { x.MaHocKy, x.TenHocKy })
                        .ToList();
                    cboHocKy.DisplayMember = "TenHocKy";
                    cboHocKy.ValueMember = "MaHocKy";

                    var khoa = db.KHOAs.OrderBy(x => x.TenKhoa)
                        .Select(x => new { x.MaKhoa, x.TenKhoa }).ToList();
                    cboKhoa.Items.Clear();
                    cboKhoa.Items.Add(new KhoaFilterItem { MaKhoa = 0, TenKhoa = "Tất cả khoa" });
                    foreach (var item in khoa)
                        cboKhoa.Items.Add(new KhoaFilterItem { MaKhoa = item.MaKhoa, TenKhoa = item.TenKhoa });
                    cboKhoa.DisplayMember = "TenKhoa";
                    cboKhoa.ValueMember = "MaKhoa";
                    cboKhoa.SelectedIndex = 0;

                    if (cboNamHoc.Items.Count > 0) cboNamHoc.SelectedIndex = 0;
                    if (cboHocKy.Items.Count > 0) cboHocKy.SelectedIndex = 0;
                }

                dangNapBoLoc = false;
                LoadWorkload();
            }
            catch (Exception ex)
            {
                dangNapBoLoc = false;
                MessageBox.Show("Không tải được dữ liệu khối lượng giảng dạy. Hãy kiểm tra SQL Server và chuỗi kết nối trong App.config.\n\nChi tiết: " + ex.Message,
                    "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadWorkload()
        {
            if (dangNapBoLoc || cboNamHoc.SelectedValue == null || cboHocKy.SelectedValue == null) return;

            try
            {
                int maNamHoc = Convert.ToInt32(cboNamHoc.SelectedValue);
                int maHocKy = Convert.ToInt32(cboHocKy.SelectedValue);
                int maKhoa = cboKhoa.SelectedItem is KhoaFilterItem
                    ? ((KhoaFilterItem)cboKhoa.SelectedItem).MaKhoa : 0;

                using (var db = new QuanLyGiangVienDataContext())
                {
                    // Chỉ dùng LINQ to SQL: lấy các phân công thuộc năm học/học kỳ đã chọn,
                    // sau đó gom theo giảng viên và cộng số tiết.
                    var assignments = db.PHAN_CONG_GIANG_DAYs
                        .Where(x => x.LOP_HOC_PHAN.MaNamHoc == maNamHoc
                                 && x.LOP_HOC_PHAN.MaHocKy == maHocKy);

                    if (maKhoa > 0)
                        assignments = assignments.Where(x => x.GIANG_VIEN.BO_MON.MaKhoa == maKhoa);

                    var totals = assignments
                        .GroupBy(x => new
                        {
                            x.MaGV,
                            x.GIANG_VIEN.HoTen,
                            TenKhoa = x.GIANG_VIEN.BO_MON.KHOA.TenKhoa
                        })
                        .Select(g => new
                        {
                            g.Key.MaGV,
                            g.Key.HoTen,
                            g.Key.TenKhoa,
                            TongTiet = g.Sum(x => x.SoTiet)
                        })
                        .OrderBy(x => x.MaGV)
                        .ToList();

                    dgvTeachingWorkload.Rows.Clear();
                    foreach (var row in totals)
                    {
                        string trangThai = row.TongTiet > DinhMucTietMoiHocKy ? "Vượt"
                            : row.TongTiet < DinhMucTietMoiHocKy ? "Thiếu" : "Đạt";
                        dgvTeachingWorkload.Rows.Add(row.MaGV, row.HoTen, row.TenKhoa,
                            row.TongTiet, DinhMucTietMoiHocKy, trangThai);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tính được khối lượng giảng dạy.\n\nChi tiết: " + ex.Message,
                    "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnThongKe_Click(object sender, EventArgs e)
        {
            LoadWorkload();
        }

        private void btnLamMoi_Click(object sender, EventArgs e)
        {
            dangNapBoLoc = true;
            if (cboNamHoc.Items.Count > 0) cboNamHoc.SelectedIndex = 0;
            if (cboHocKy.Items.Count > 0) cboHocKy.SelectedIndex = 0;
            if (cboKhoa.Items.Count > 0) cboKhoa.SelectedIndex = 0;
            dangNapBoLoc = false;
            LoadWorkload();
        }

        private static bool LicenseManagerIsDesignTime()
        {
            return System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime;
        }

        private void dgvTeachingWorkload_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        // Giữ nguyên các event handler do WinForms Designer đang tham chiếu.
        private void lblNamHoc_Click(object sender, EventArgs e) { }
        private void cboNamHoc_SelectedIndexChanged(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void lblHocKy_Click(object sender, EventArgs e) { }

        private sealed class KhoaFilterItem
        {
            public int MaKhoa { get; set; }
            public string TenKhoa { get; set; }
        }
    }
}
