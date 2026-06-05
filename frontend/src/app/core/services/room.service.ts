import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Room {
  id: number;
  name: string;
  // FIX: Backend RoomDto.Capacity is int? (nullable) — typed correctly to avoid
  //      "null" being displayed in the UI or Number(null)=0 failing validation.
  capacity: number | null;
  type: string; // e.g., 'Classroom', 'ScienceLab', 'ComputerLab' ...
}

@Injectable({
  providedIn: 'root'
})
export class RoomService {
  private http = inject(HttpClient);
  private apiUrl = '/api/rooms';

  getAll(): Observable<Room[]> {
    return this.http.get<Room[]>(this.apiUrl);
  }

  getById(id: number): Observable<Room> {
    return this.http.get<Room>(`${this.apiUrl}/${id}`);
  }

  getByType(type: string): Observable<Room[]> {
    const params = new HttpParams().set('type', type);
    return this.http.get<Room[]>(`${this.apiUrl}/by-type`, { params });
  }

  getAvailable(day: string, periodNumber: number, type?: string): Observable<Room[]> {
    let params = new HttpParams()
      .set('day', day)
      .set('periodNumber', periodNumber.toString());

    if (type) {
      params = params.set('type', type);
    }

    return this.http.get<Room[]>(`${this.apiUrl}/available`, { params });
  }

  getSchedule(id: number, day?: string): Observable<any> {
    let params = new HttpParams();
    if (day) {
      params = params.set('day', day);
    }
    return this.http.get<any>(`${this.apiUrl}/${id}/schedule`, { params });
  }

  create(data: Partial<Room>): Observable<Room> {
    return this.http.post<Room>(this.apiUrl, data);
  }

  // FIX: RoomsController.Update checks  `if (id != request.Id) → BadRequest`.
  //      Previously the body was sent without `id`, so request.Id defaulted to 0
  //      and the check ALWAYS failed.  Now we spread `id` into the body so both
  //      the URL segment and the JSON body carry the same value.
  update(id: number, data: Partial<Room>): Observable<Room> {
    return this.http.put<Room>(`${this.apiUrl}/${id}`, { ...data, id });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
