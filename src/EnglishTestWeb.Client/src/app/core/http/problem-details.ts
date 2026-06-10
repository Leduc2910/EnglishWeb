export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  extensions?: {
    code?: string;
  };
}

export function readProblemCode(problem: ApiProblemDetails): string | undefined {
  const code = problem.code ?? problem.extensions?.code;
  return code || undefined;
}

export function isProblemDetails(value: unknown): value is ApiProblemDetails {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return false;
  }

  const problem = value as ApiProblemDetails;
  return (
    typeof problem.title === 'string' ||
    typeof problem.type === 'string' ||
    typeof problem.status === 'number' ||
    typeof problem.detail === 'string' ||
    typeof problem.code === 'string' ||
    typeof problem.extensions?.code === 'string'
  );
}
