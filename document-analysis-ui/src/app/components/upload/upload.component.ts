import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [
    CommonModule,
    HttpClientModule
  ],
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss']
})
export class UploadComponent {
  selectedFile!: File;
  response: any;
  loading = false;
  errorMessage: string | null = null; // ✅ adăugat

  constructor(private http: HttpClient) {}

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
    this.errorMessage = null; // ✅ resetează eroarea la selectare nouă
  }

  upload() {
    if (!this.selectedFile) {
      this.errorMessage = 'Te rog sa incarci un document inainte de a continua.';
      return;
    }

    const formData = new FormData();
    formData.append('file', this.selectedFile);

    this.loading = true;
    this.errorMessage = null;

    this.http.post('https://localhost:7297/api/documents/upload-comparison', formData)
      .subscribe({
        next: (res) => {
          this.response = res;
          this.loading = false;
        },
        error: (err) => {
          this.loading = false;
          this.response = null;
          this.errorMessage = err?.error?.error || 'A aparut o eroare in timpul ncrcarii.';
          console.error('Upload error', err);
        }
      });
  }
}