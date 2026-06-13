import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface TtsResponse {
  isSuccess: boolean;
}

@Injectable({ providedIn: 'root' })
export class TextToSpeechService {
  private http = inject(HttpClient);

  /** إرسال النص إلى الباك اند وتحويله إلى صوت */
  synthesizeSpeech(text: string): Observable<Blob> {
    return this.http.post(
      buildApiUrl('ai/tts'),
      { text },
      { responseType: 'blob' }
    );
  }
}
