import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface TranscribeResult {
  isSuccess: boolean;
  text?: string;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class VoiceTranscriptionService {
  private http = inject(HttpClient);

  /** إرسال ملف صوتي للباك اند واستخراج النص */
  transcribeAudio(audioBlob: Blob, fileName = 'recording.webm'): Observable<TranscribeResult> {
    const formData = new FormData();
    formData.append('file', audioBlob, fileName);
    return this.http.post<TranscribeResult>(buildApiUrl('ai/transcribe'), formData);
  }

  /** هل المتصفح يدعم Web Speech API؟ */
  static isBrowserSpeechSupported(): boolean {
    return !!(
      (window as any).SpeechRecognition ||
      (window as any).webkitSpeechRecognition
    );
  }

  /** هل المتصفح يدعم MediaRecorder API؟ */
  static isMediaRecorderSupported(): boolean {
    return !!(
      typeof navigator.mediaDevices?.getUserMedia === 'function' &&
      typeof (window as any).MediaRecorder === 'function'
    );
  }
}
