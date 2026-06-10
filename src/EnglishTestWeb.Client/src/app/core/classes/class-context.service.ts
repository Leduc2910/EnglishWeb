import { Injectable, signal } from '@angular/core';
import { ActiveClass, ClassLookupPreview } from './classes.models';
import { normalizeClassCode } from './class-code';

const SESSION_CLASS_CODE_KEY = 'EnglishTestWeb.ClassCode';
const SESSION_CLASS_CONFIRMED_KEY = 'EnglishTestWeb.ClassConfirmed';

@Injectable({ providedIn: 'root' })
export class ClassContextService {
  private readonly confirmedClassSignal = signal<ClassLookupPreview | null>(null);
  private readonly activeClassSignal = signal<ActiveClass | null>(null);

  readonly confirmedClass = this.confirmedClassSignal.asReadonly();
  readonly activeClass = this.activeClassSignal.asReadonly();

  setConfirmedClass(preview: ClassLookupPreview): void {
    this.confirmedClassSignal.set(preview);
    this.persistClassCode(preview.classCode);
    this.markClassConfirmed(preview.classCode);
  }

  setActiveClass(activeClass: ActiveClass): void {
    this.activeClassSignal.set(activeClass);
    this.persistClassCode(activeClass.classCode);
    this.markClassConfirmed(activeClass.classCode);
  }

  clearClassContext(): void {
    this.confirmedClassSignal.set(null);
    this.activeClassSignal.set(null);
    sessionStorage.removeItem(SESSION_CLASS_CODE_KEY);
    this.clearClassConfirmed();
  }

  persistClassCode(classCode: string): void {
    sessionStorage.setItem(SESSION_CLASS_CODE_KEY, classCode);
  }

  readPersistedClassCode(): string | null {
    return sessionStorage.getItem(SESSION_CLASS_CODE_KEY);
  }

  isConfirmedForClass(classCode: string): boolean {
    const confirmed = sessionStorage.getItem(SESSION_CLASS_CONFIRMED_KEY);
    if (!confirmed) {
      return false;
    }

    const normalized = normalizeClassCode(classCode) ?? classCode.trim().toUpperCase();
    return confirmed === normalized;
  }

  markClassConfirmed(classCode: string): void {
    const normalized = normalizeClassCode(classCode) ?? classCode.trim().toUpperCase();
    sessionStorage.setItem(SESSION_CLASS_CONFIRMED_KEY, normalized);
  }

  private clearClassConfirmed(): void {
    sessionStorage.removeItem(SESSION_CLASS_CONFIRMED_KEY);
  }
}
