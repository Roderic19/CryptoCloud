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
  public aiResponse = signal('Response:');
  private backendUrl: string = environment.apiUrl;

  constructor(private httpClient: HttpClient) {}

  onTestClick() {
    this.httpClient.post<ChatResponse>(this.backendUrl + '/Chat', {}).subscribe({
      next: (data) => {
        this.aiResponse.set(data.message || 'No response returned.');
      },
      error: (err) => {
        this.aiResponse.set(err.error?.error ?? err.message ?? 'Unable to contact the backend.');
      },
    });
  }
}
