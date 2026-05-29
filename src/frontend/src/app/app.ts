import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

interface ChatResponse {
  message: string;
}

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  //protected readonly title = signal('CryptoCloud-Frontend');
  private aiResponse: string = '';
  private backendUrl: string = environment.apiUrl;

  constructor(private httpClient: HttpClient) {}

  onTestClick() {
    this.httpClient.post<ChatResponse>(this.backendUrl + '/api/Chat', {}).subscribe({
      next: data => {
        this.aiResponse = data.message;
      },
      error: (err) => {
        this.aiResponse = 'Error: ' + err;
      },
    });
  }
}
