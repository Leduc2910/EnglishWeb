import { APIRequestContext } from '@playwright/test';
import { MINIMAL_PDF_BYTES, MINIMAL_WEBM_BYTES } from './test-files';

export const TEACHER_EMAIL = 'teacher@englishtestweb.local';
export const TEACHER_PASSWORD = 'Teacher123!';
export const STUDENT_IDENTIFIER = 'student@englishtestweb.local';
export const STUDENT_PASSWORD = 'Student123!';
export const CLASS_CODE = 'ENG7A';

async function getXsrfToken(api: APIRequestContext): Promise<string> {
  const res = await api.get('/api/security/xsrf-token');
  if (!res.ok()) throw new Error(`Failed to get XSRF token: ${res.status()}`);
  const body = await res.json();
  return body.requestToken as string;
}

export async function loginTeacher(api: APIRequestContext): Promise<string> {
  const xsrfToken = await getXsrfToken(api);
  const res = await api.post('/api/auth/login', {
    data: { identifier: TEACHER_EMAIL, password: TEACHER_PASSWORD, rememberMe: false },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Teacher login failed: ${res.status()} ${await res.text()}`);
  return xsrfToken;
}

export async function loginStudentWithClass(
  api: APIRequestContext,
  classCode: string,
): Promise<string> {
  const xsrfToken = await getXsrfToken(api);
  const res = await api.post('/api/auth/student/login', {
    data: {
      identifier: STUDENT_IDENTIFIER,
      password: STUDENT_PASSWORD,
      classCode,
      rememberMe: false,
    },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Student login failed: ${res.status()} ${await res.text()}`);
  return xsrfToken;
}

export async function getClassIdByCode(
  api: APIRequestContext,
  code: string,
): Promise<string> {
  const res = await api.get(`/api/classes/by-code/${code}`);
  if (!res.ok()) throw new Error(`Class lookup failed: ${res.status()}`);
  const body = await res.json();
  return body.classId as string;
}

export async function createReadyReadingTemplate(
  api: APIRequestContext,
  xsrfToken: string,
): Promise<string> {
  const uniqueName = `E2E Reading ${Date.now()}`;

  const createRes = await api.post('/api/test-templates', {
    data: { title: uniqueName, skill: 'reading' },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!createRes.ok()) throw new Error(`Create template failed: ${createRes.status()} ${await createRes.text()}`);
  const { id: templateId } = await createRes.json();

  const uploadRes = await api.post(`/api/test-templates/${templateId}/materials`, {
    multipart: {
      file: {
        name: 'test.pdf',
        mimeType: 'application/pdf',
        buffer: MINIMAL_PDF_BYTES,
      },
      role: 'pdf',
    },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!uploadRes.ok()) throw new Error(`Upload material failed: ${uploadRes.status()} ${await uploadRes.text()}`);

  const answerKeyRes = await api.put(`/api/test-templates/${templateId}/answer-key`, {
    data: {
      questionCount: 3,
      scoringMode: 'equal',
      totalScore: 10,
      rows: [
        { questionNumber: 1, correctAnswer: 'A', score: null },
        { questionNumber: 2, correctAnswer: 'B', score: null },
        { questionNumber: 3, correctAnswer: 'C', score: null },
      ],
    },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!answerKeyRes.ok()) throw new Error(`Set answer key failed: ${answerKeyRes.status()} ${await answerKeyRes.text()}`);

  const markReadyRes = await api.post(`/api/test-templates/${templateId}/mark-ready`, {
    data: {},
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!markReadyRes.ok()) throw new Error(`Mark ready failed: ${markReadyRes.status()} ${await markReadyRes.text()}`);

  return templateId as string;
}

export async function createReadySpeakingTemplate(
  api: APIRequestContext,
  xsrfToken: string,
): Promise<string> {
  const uniqueName = `E2E Speaking ${Date.now()}`;

  const createRes = await api.post('/api/test-templates', {
    data: { title: uniqueName, skill: 'speaking' },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!createRes.ok()) throw new Error(`Create speaking template failed: ${createRes.status()} ${await createRes.text()}`);
  const { id: templateId } = await createRes.json();

  const markReadyRes = await api.post(`/api/test-templates/${templateId}/mark-ready`, {
    data: {},
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!markReadyRes.ok()) throw new Error(`Mark speaking template ready failed: ${markReadyRes.status()} ${await markReadyRes.text()}`);

  return templateId as string;
}

export async function createHomeworkAssignment(
  api: APIRequestContext,
  xsrfToken: string,
  templateId: string,
  classId: string,
): Promise<string> {
  const deadlineAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
  const res = await api.post('/api/homework-assignments', {
    data: { templateId, classId, deadlineAt },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Create homework failed: ${res.status()} ${await res.text()}`);
  const body = await res.json();
  return body.id as string;
}

export async function createLiveExamSession(
  api: APIRequestContext,
  xsrfToken: string,
  templateId: string,
  classId: string,
): Promise<string> {
  const res = await api.post('/api/live-exam-sessions', {
    data: { templateId, classId },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Create live exam failed: ${res.status()} ${await res.text()}`);
  const body = await res.json();
  return body.id as string;
}

export async function createSpeakingSubmission(
  api: APIRequestContext,
  xsrfToken: string,
  homeworkAssignmentId: string,
): Promise<string> {
  const res = await api.post('/api/speaking-submissions', {
    data: { homeworkAssignmentId, liveExamSessionId: null },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Create speaking submission failed: ${res.status()} ${await res.text()}`);
  const body = await res.json();
  return body.id as string;
}

export async function uploadSpeakingDraft(
  api: APIRequestContext,
  xsrfToken: string,
  submissionId: string,
): Promise<void> {
  const res = await api.post(`/api/speaking-submissions/${submissionId}/upload-draft`, {
    multipart: {
      file: {
        name: 'recording.webm',
        mimeType: 'audio/webm',
        buffer: MINIMAL_WEBM_BYTES,
      },
    },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Upload speaking draft failed: ${res.status()} ${await res.text()}`);
}

export async function finalSubmitSpeaking(
  api: APIRequestContext,
  xsrfToken: string,
  submissionId: string,
): Promise<void> {
  const res = await api.post(`/api/speaking-submissions/${submissionId}/final-submit`, {
    data: {},
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Final submit speaking failed: ${res.status()} ${await res.text()}`);
}

export async function createExpiredHomeworkAssignment(
  api: APIRequestContext,
  xsrfToken: string,
  templateId: string,
  classId: string,
): Promise<string> {
  const deadlineAt = new Date(Date.now() - 5 * 60 * 1000).toISOString(); // 5 minutes in the past
  const res = await api.post('/api/homework-assignments', {
    data: { templateId, classId, deadlineAt },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Create expired homework failed: ${res.status()} ${await res.text()}`);
  const body = await res.json();
  return body.id as string;
}

export async function openLiveExamSession(
  api: APIRequestContext,
  xsrfToken: string,
  sessionId: string,
): Promise<void> {
  const res = await api.post(`/api/live-exam-sessions/${sessionId}/open`, {
    data: {},
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Open live exam session failed: ${res.status()} ${await res.text()}`);
}

export async function createReadingAttempt(
  api: APIRequestContext,
  xsrfToken: string,
  homeworkAssignmentId: string,
): Promise<string> {
  const res = await api.post('/api/submissions', {
    data: { homeworkAssignmentId, liveExamSessionId: null },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Create reading attempt failed: ${res.status()} ${await res.text()}`);
  const body = await res.json();
  return body.id as string;
}

export async function saveAnswerDraft(
  api: APIRequestContext,
  xsrfToken: string,
  submissionId: string,
  rows: Array<{ questionNumber: number; answer: string }>,
): Promise<void> {
  const res = await api.put(`/api/submissions/${submissionId}/answers`, {
    data: { rows },
    headers: { 'X-XSRF-TOKEN': xsrfToken },
  });
  if (!res.ok()) throw new Error(`Save answer draft failed: ${res.status()} ${await res.text()}`);
}

export async function seedExpiredHomeworkChain(api: APIRequestContext): Promise<{
  templateId: string;
  homeworkId: string;
}> {
  const xsrfToken = await loginTeacher(api);
  const classId = await getClassIdByCode(api, CLASS_CODE);
  const templateId = await createReadyReadingTemplate(api, xsrfToken);
  const homeworkId = await createExpiredHomeworkAssignment(api, xsrfToken, templateId, classId);
  return { templateId, homeworkId };
}

export async function seedNotSubmittedReadingChain(api: APIRequestContext): Promise<{
  submissionId: string;
  homeworkId: string;
}> {
  const teacherXsrf = await loginTeacher(api);
  const classId = await getClassIdByCode(api, CLASS_CODE);
  const templateId = await createReadyReadingTemplate(api, teacherXsrf);
  const homeworkId = await createHomeworkAssignment(api, teacherXsrf, templateId, classId);

  const studentXsrf = await loginStudentWithClass(api, CLASS_CODE);
  const submissionId = await createReadingAttempt(api, studentXsrf, homeworkId);
  // Save 2 of 3 answers so answers can be verified as restored on reload
  await saveAnswerDraft(api, studentXsrf, submissionId, [
    { questionNumber: 1, answer: 'A' },
    { questionNumber: 2, answer: 'B' },
  ]);

  return { submissionId, homeworkId };
}

export async function seedSubmittedSpeakingChain(api: APIRequestContext): Promise<{
  templateId: string;
  homeworkId: string;
  submissionId: string;
}> {
  const teacherXsrf = await loginTeacher(api);
  const classId = await getClassIdByCode(api, CLASS_CODE);
  const templateId = await createReadySpeakingTemplate(api, teacherXsrf);
  const homeworkId = await createHomeworkAssignment(api, teacherXsrf, templateId, classId);

  // Need separate API context for student to avoid cookie collision
  // The student login and upload is done via API using student credentials
  // We reuse the same api context - but student auth overwrites teacher auth in the same context
  // That's acceptable here since we do teacher work first, then student work

  const studentXsrf = await loginStudentWithClass(api, CLASS_CODE);
  const submissionId = await createSpeakingSubmission(api, studentXsrf, homeworkId);
  await uploadSpeakingDraft(api, studentXsrf, submissionId);
  await finalSubmitSpeaking(api, studentXsrf, submissionId);

  return { templateId, homeworkId, submissionId };
}
