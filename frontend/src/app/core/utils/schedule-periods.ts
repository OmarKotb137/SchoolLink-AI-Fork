import { TimetableSlotDto, TeacherScheduleSlotDto } from '../models/timetable.models';

export interface SchedulePeriodView {
  num: number;
  label: string;
  start: string;
  end: string;
}

export const DEFAULT_SCHEDULE_PERIODS: SchedulePeriodView[] = [
  { num: 1, label: 'الأولى', start: '08:00', end: '08:45' },
  { num: 2, label: 'الثانية', start: '08:45', end: '09:30' },
  { num: 3, label: 'الثالثة', start: '09:30', end: '10:15' },
  { num: 4, label: 'الرابعة', start: '10:30', end: '11:15' },
  { num: 5, label: 'الخامسة', start: '11:15', end: '12:00' },
  { num: 6, label: 'السادسة', start: '12:00', end: '12:45' },
  { num: 7, label: 'السابعة', start: '12:45', end: '13:30' },
];

type ScheduleSlotLike = TimetableSlotDto | TeacherScheduleSlotDto;

export function buildSchedulePeriods(slots: ScheduleSlotLike[]): SchedulePeriodView[] {
  if (!slots.length) {
    return DEFAULT_SCHEDULE_PERIODS;
  }

  const byPeriod = new Map<number, SchedulePeriodView>();

  for (const slot of slots) {
    if (!byPeriod.has(slot.periodNumber)) {
      byPeriod.set(slot.periodNumber, {
        num: slot.periodNumber,
        label: getArabicPeriodLabel(slot.periodNumber),
        start: toHourMinute(slot.startTime),
        end: toHourMinute(slot.endTime),
      });
    }
  }

  return [...byPeriod.values()].sort((a, b) => a.num - b.num);
}

export function getCurrentPeriodNumber(periods: SchedulePeriodView[], now: Date = new Date()): number | null {
  const nowMinutes = now.getHours() * 60 + now.getMinutes();

  for (const period of periods) {
    const [startHour, startMinute] = period.start.split(':').map(Number);
    const [endHour, endMinute] = period.end.split(':').map(Number);
    const start = startHour * 60 + startMinute;
    const end = endHour * 60 + endMinute;

    if (nowMinutes >= start && nowMinutes < end) {
      return period.num;
    }
  }

  return null;
}

function toHourMinute(value: string): string {
  return value.slice(0, 5);
}

function getArabicPeriodLabel(periodNumber: number): string {
  const labels: Record<number, string> = {
    1: 'الأولى',
    2: 'الثانية',
    3: 'الثالثة',
    4: 'الرابعة',
    5: 'الخامسة',
    6: 'السادسة',
    7: 'السابعة',
    8: 'الثامنة',
    9: 'التاسعة',
    10: 'العاشرة',
  };

  return labels[periodNumber] ?? `الحصة ${periodNumber}`;
}
